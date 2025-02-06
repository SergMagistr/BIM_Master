using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace DeleteMaterial
{
    [Transaction(TransactionMode.Manual)]
    public class DeleteUnusedMaterials : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Проверяем, что это файл семейства
            if (!doc.IsFamilyDocument)
            {
                TaskDialog.Show("Ошибка", "Этот плагин работает только в файле семейства.");
                return Result.Failed;
            }

            // 1. Собираем все вложенные семейства и их параметры материалов
            HashSet<ElementId> usedMaterialIds = GetUsedMaterialIds(doc);

            // 2. Собираем все материалы, используемые во всех элементах проекта
            usedMaterialIds.UnionWith(GetUsedMaterialIdsFromAllElements(doc));

            // 3. Собираем все материалы в документе
            HashSet<ElementId> allMaterialIds = GetAllMaterialIds(doc);

            // 4. Определяем, какие материалы используются и какие нет
            HashSet<ElementId> materialsToKeep = new HashSet<ElementId>(usedMaterialIds.Intersect(allMaterialIds));
            HashSet<ElementId> materialsToDelete = new HashSet<ElementId>(allMaterialIds.Except(materialsToKeep));

            // 5. Удаляем неиспользуемые материалы
            int deletedCount = DeleteMaterials(doc, materialsToDelete);

            // 6. Повторный подсчет оставшихся материалов
            int remainingMaterialsCount = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .GetElementCount();

            TaskDialog.Show("Результат", $"Удалено неиспользуемых материалов: {deletedCount}\nОсталось материалов: {remainingMaterialsCount}");

            return Result.Succeeded;
        }

        /// <summary>
        /// Собираем все ID используемых материалов в параметрах вложенных семейств и типов семейств
        /// </summary>
        private HashSet<ElementId> GetUsedMaterialIds(Document doc)
        {
            HashSet<ElementId> usedMaterialIds = new HashSet<ElementId>();

            // Получаем все элементы (включая вложенные семейства)
            FilteredElementCollector elementCollector = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType();

            foreach (Element element in elementCollector)
            {
                // Проверяем стандартный параметр материала
                Parameter materialParam = element.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                if (materialParam != null && materialParam.HasValue)
                {
                    usedMaterialIds.Add(materialParam.AsElementId());
                }

                // Если элемент - вложенное семейство (FamilyInstance), проверяем его параметры
                if (element is FamilyInstance familyInstance)
                {
                    FamilySymbol symbol = familyInstance.Symbol;
                    if (symbol != null)
                    {
                        foreach (Parameter param in symbol.Parameters)
                        {
                            if (param.Definition.ParameterType == ParameterType.Material && param.HasValue)
                            {
                                usedMaterialIds.Add(param.AsElementId());
                            }
                        }
                    }
                }
            }
            return usedMaterialIds;
        }

        /// <summary>
        /// Собираем ID всех материалов, используемых во всех элементах проекта
        /// </summary>
        private HashSet<ElementId> GetUsedMaterialIdsFromAllElements(Document doc)
        {
            HashSet<ElementId> usedMaterialIds = new HashSet<ElementId>();

            // Получаем все элементы модели
            FilteredElementCollector elementCollector = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType();

            foreach (Element element in elementCollector)
            {
                foreach (Parameter param in element.Parameters)
                {
                    if (param.Definition.ParameterType == ParameterType.Material && param.HasValue)
                    {
                        usedMaterialIds.Add(param.AsElementId());
                    }
                }
            }
            return usedMaterialIds;
        }

        /// <summary>
        /// Собираем все ID материалов в документе
        /// </summary>
        private HashSet<ElementId> GetAllMaterialIds(Document doc)
        {
            FilteredElementCollector materialCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(Material));

            return new HashSet<ElementId>(materialCollector.Select(m => m.Id));
        }

        /// <summary>
        /// Удаляем неиспользуемые материалы
        /// </summary>
        private int DeleteMaterials(Document doc, HashSet<ElementId> materialsToDelete)
        {
            int deletedCount = 0;

            using (Transaction trans = new Transaction(doc, "Удаление неиспользуемых материалов"))
            {
                trans.Start();
                foreach (ElementId materialId in materialsToDelete)
                {
                    doc.Delete(materialId);
                    deletedCount++;
                }
                trans.Commit();
            }

            return deletedCount;
        }
    }
}
