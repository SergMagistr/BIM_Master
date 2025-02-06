using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

public class FamilyParameterAnalyzer
{
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

        return referencedParameters.ToList();
    }
}
