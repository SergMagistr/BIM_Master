using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;


// Метод который ищет в семействе встроенные параметры 
public class GetBuiltInParameters
{
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
}
