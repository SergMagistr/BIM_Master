using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;


// Метод который ищет параметры которые связанны со вложенными параметрами 
public class AssociatedParameterFinder
{
    /// <summary>
    /// Находит параметры в родительском семействе, которые связаны с параметрами вложенных семейств.
    /// </summary>
    /// <param name="doc">Текущий документ семейства</param>
    /// <returns>Список кортежей (Родительский параметр, Вложенное семейство, Параметр во вложенном семействе)</returns>
    public static List<(string ParentParam, string NestedFamily, string NestedParam)> GetAssociatedParameters(Document doc)
    {
        List<(string ParentParam, string NestedFamily, string NestedParam)> linkedParameters = new List<(string, string, string)>();

        if (doc == null || !doc.IsFamilyDocument)
            throw new InvalidOperationException("Метод должен вызываться только в файле семейства.");

        // Получаем все вложенные семейства (FamilyInstance)
        var familyInstances = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>();

        FamilyManager familyManager = doc.FamilyManager;

        foreach (var instance in familyInstances)
        {
            FamilySymbol symbol = instance.Symbol;
            if (symbol == null) continue;

            foreach (Parameter nestedParam in instance.Parameters)
            {
                // Используем GetAssociatedFamilyParameter для поиска родительского параметра
                FamilyParameter parentParam = doc.FamilyManager.GetAssociatedFamilyParameter(nestedParam);
                if (parentParam != null)
                {
                    linkedParameters.Add((parentParam.Definition.Name, symbol.Family.Name, nestedParam.Definition.Name));
                }
            }
        }

        return linkedParameters.Distinct().ToList();
    }
}
