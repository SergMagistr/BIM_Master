using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;



public class FamilyParameterUtils
{
    // Метод для получения всех параметров семейства
    public static List<FamilyParameter> GetNonSharedFamilyParameters(Document doc)
    {
        List<FamilyParameter> familyParameters = new List<FamilyParameter>();

        // Проверяем, что документ - это семейство
        if (!doc.IsFamilyDocument)
        {
            TaskDialog.Show("Ошибка", "Документ не является файлом семейства.");
            return familyParameters;
        }

        // Получаем менеджер параметров семейства
        FamilyManager familyManager = doc.FamilyManager;

        // Перебираем параметры и добавляем только не общие
        foreach (FamilyParameter param in familyManager.Parameters)
        {
            if (!param.IsShared)
            {
                familyParameters.Add(param);
            }
        }

        return familyParameters;
    }

    // Метод для получения встроенных параметров семейства
    public static List<FamilyParameter> GetBuiltInFamilyParameters(Document doc)
    {
        List<FamilyParameter> builtInParams = new List<FamilyParameter>();

        // Проверяем, что документ является файлом семейства
        if (!doc.IsFamilyDocument)
        {
            return builtInParams; // Возвращаем пустой список, если это не семейство
        }

        FamilyManager familyManager = doc.FamilyManager;

        foreach (FamilyParameter param in familyManager.Parameters)
        {
            // Фильтруем только встроенные параметры
            if (!param.IsShared && param.Definition is InternalDefinition internalDef &&
                internalDef.BuiltInParameter != BuiltInParameter.INVALID)
            {
                builtInParams.Add(param);
            }
        }

        return builtInParams;
    }

    // Метод для получения параметров, связанных с размерами
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

    // Метод для поиска параметров, связанных с вложенными семействами
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

    // Метод для получения связанных параметров семейства
    public static List<string> GetAssociatedFamilyParameters(Document doc)
    {
        List<string> linkedParams = new List<string>();

        // Проверяем, что документ является файлом семейства
        if (!doc.IsFamilyDocument)
        {
            return linkedParams;
        }

        FamilyManager famManager = doc.FamilyManager;

        // Получаем все параметры семейства
        foreach (FamilyParameter famParam in famManager.Parameters)
        {
            // Проверяем, есть ли у параметра связь с геометрией (ParameterSet)
            ParameterSet associatedParams = famParam.AssociatedParameters;
            if (associatedParams != null && associatedParams.Size > 0)
            {
                linkedParams.Add(famParam.Definition.Name);
            }
        }

        return linkedParams;
    }


    // Метод для получения параметров, участвующих в формулах
    public static List<string> GetReferencedParameters(Document doc)
    {
        if (!doc.IsFamilyDocument)
        {
            throw new InvalidOperationException("Документ не является семейством.");
        }

        FamilyManager familyManager = doc.FamilyManager;
        Dictionary<string, FamilyParameter> allParameters = familyManager.Parameters
            .Cast<FamilyParameter>()
            .ToDictionary(p => p.Definition.Name, p => p);

        HashSet<string> referencedParameters = new HashSet<string>();

        // 1. Собираем параметры, которые имеют формулы
        foreach (FamilyParameter param in allParameters.Values)
        {
            string formula = param.Formula;
            if (!string.IsNullOrEmpty(formula))
            {
                referencedParameters.Add(param.Definition.Name);
            }
        }

        // 2. Собираем параметры, участвующие в формулах
        foreach (FamilyParameter param in allParameters.Values)
        {
            string formula = param.Formula;
            if (!string.IsNullOrEmpty(formula))
            {
                foreach (var paramName in allParameters.Keys)
                {
                    if (formula.Contains(paramName))
                    {
                        referencedParameters.Add(paramName);
                    }
                }
            }
        }

        // 3. Получаем дополнительные списки используемых параметров

        // 3.1 Параметры, связанные с размерами (используемые в метках)
        var linkedParams = FamilyParameterUtils.GetLinkedFamilyParameters(doc)
                           .Select(p => p.Definition.Name);

        // 3.2 Параметры, связанные с вложенными семействами или геометрическими объектами
        var associatedParams = FamilyParameterUtils.GetAssociatedFamilyParameters(doc);

        // Объединяем оба списка в один набор
        var extraUsedParams = new HashSet<string>(linkedParams.Union(associatedParams));

        // 4. Фильтруем: оставляем только те параметры, которые есть в объединённом списке extraUsedParams
        referencedParameters = new HashSet<string>(referencedParameters.Where(p => extraUsedParams.Contains(p)));

        return referencedParameters.ToList();
    }
}
