using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;

// Метод , который собирает все параетры семейства
// 
public class GetFamilyParameters
{
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
}
