using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

// Метод котоырй ищет параметры , которые связаны с размерами 

public class FamilyParameterUtils
{
    public static List<FamilyParameter> GetLinkedFamilyParameters(Document doc)
    {
        List<FamilyParameter> linkedParameters = new List<FamilyParameter>();

        // Проверяем, что документ - это семейство
        if (!doc.IsFamilyDocument)
        {
            return linkedParameters;
        }

        // Создаем HashSet для хранения ID параметров, привязанных к меткам
        HashSet<int> linkedParamIds = new HashSet<int>();

        // Получаем все размеры (Dimension) в семействе
        IEnumerable<Dimension> dimensions = new FilteredElementCollector(doc)
            .OfClass(typeof(Dimension))
            .WhereElementIsNotElementType()
            .Cast<Dimension>();

        foreach (Dimension dim in dimensions)
        {
            try
            {
                // Если у размера есть привязанная метка (FamilyLabel), добавляем ее ID в HashSet
                if (dim != null && dim.FamilyLabel != null)
                {
                    linkedParamIds.Add(dim.FamilyLabel.Id.IntegerValue);
                }
            }
            catch
            {
                continue; // Игнорируем ошибки
            }
        }

        // Получаем все параметры семейства
        foreach (FamilyParameter param in doc.FamilyManager.Parameters)
        {
            if (linkedParamIds.Contains(param.Id.IntegerValue))
            {
                linkedParameters.Add(param);
            }
        }

        return linkedParameters;
    }
}
