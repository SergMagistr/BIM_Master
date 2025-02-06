using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

[Transaction(TransactionMode.Manual)]
public class UnusedFamilyParametersCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;
        Document doc = uidoc.Document;

        if (!doc.IsFamilyDocument)
        {
            TaskDialog.Show("Ошибка", "Этот инструмент работает только в редакторе семейств.");
            return Result.Failed;
        }

        using (Transaction tx = new Transaction(doc, "Анализ параметров семейства"))
        {
            tx.Start();

            // 1. Получаем все параметры семейства
            List<FamilyParameter> allParams = GetFamilyParameters.GetNonSharedFamilyParameters(doc);

            // 2. Получаем встроенные параметры
            List<FamilyParameter> builtInParams = GetBuiltInParameters.GetBuiltInFamilyParameters(doc);

            // 3. Получаем связанные параметры (используемые в метках)
            List<FamilyParameter> linkedParams = FamilyParameterUtils.GetLinkedFamilyParameters(doc);

            // 4. Получаем параметры, связанные с вложенными семействами
            List<(string ParentParam, string NestedFamily, string NestedParam)> associatedParams =
                AssociatedParameterFinder.GetAssociatedParameters(doc);

            // 5. Получаем параметры, участвующие в формулах (метод уже есть в другом файле)
            List<string> formulaReferencedParams = FamilyParameterAnalyzer.GetReferencedParameters(doc);

            // 6. Создаем **уникальный список** параметров, которые нужно исключить
            HashSet<string> excludedParamNames = new HashSet<string>(
                builtInParams.Select(p => p.Definition.Name)
                .Concat(linkedParams.Select(p => p.Definition.Name))
                .Concat(associatedParams.Select(p => p.ParentParam)) // Параметры, связанные с вложенными семействами
                .Concat(formulaReferencedParams) // Добавляем параметры, участвующие в формулах
            );

            // 7. Вычитаем параметры: оставляем только те, которых нет в excludedParamNames
            List<FamilyParameter> unusedParams = allParams
                .Where(p => !excludedParamNames.Contains(p.Definition.Name))
                .ToList();

            // 8. Открываем форму с результатами
            ParameterTableForm tableForm = new ParameterTableForm(unusedParams, "Ненужные параметры");
            tableForm.ShowDialog();

            tx.Commit();
        }

        return Result.Succeeded;
    }
}

/// <summary>Форма с таблицей ненужных параметров.</summary>
public class ParameterTableForm : System.Windows.Forms.Form
{
    public ParameterTableForm(List<FamilyParameter> parameters, string title)
    {
        this.Text = title;
        this.Width = 500;
        this.Height = 600;
        this.StartPosition = FormStartPosition.CenterScreen;

        TableLayoutPanel layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            AutoSize = true
        };

        if (!parameters.Any())
        {
            Label noDataLabel = new Label
            {
                Text = "Нет ненужных параметров.",
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            layout.Controls.Add(noDataLabel, 0, 0);
        }
        else
        {
            DataGridView gridView = CreateGridView("Название параметра", "ID");

            foreach (var param in parameters)
            {
                gridView.Rows.Add(param.Definition.Name, param.Id.IntegerValue.ToString());
            }

            layout.Controls.Add(gridView, 0, 1);
        }

        this.Controls.Add(layout);
    }

    private DataGridView CreateGridView(params string[] columnNames)
    {
        DataGridView gridView = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };

        foreach (string col in columnNames)
        {
            gridView.Columns.Add(col.Replace(" ", ""), col);
        }

        return gridView;
    }
}
