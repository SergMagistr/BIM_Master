using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

// Разрешаем неоднозначность с Panel
using WinFormsPanel = System.Windows.Forms.Panel;

[Transaction(TransactionMode.Manual)]
public class UnusedFamilyParametersCommand1 : IExternalCommand
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
            List<FamilyParameter> allParams = FamilyParameterUtils.GetNonSharedFamilyParameters(doc);

            // 2. Получаем встроенные параметры
            List<FamilyParameter> builtInParams = FamilyParameterUtils.GetBuiltInFamilyParameters(doc);

            // 3. Получаем связанные параметры (используемые в метках)
            List<FamilyParameter> linkedParams = FamilyParameterUtils.GetLinkedFamilyParameters(doc);

            // 4. Получаем параметры, связанные с вложенными семействами
            List<string> associatedParams = FamilyParameterUtils.GetAssociatedFamilyParameters(doc); 

            // 5. Получаем параметры, участвующие в формулах
            List<string> formulaReferencedParams = FamilyParameterUtils.GetReferencedParameters(doc);
            
            // 6. Создаем уникальный список параметров, которые нужно исключить
            HashSet<string> excludedParamNames = new HashSet<string>(
                builtInParams.Select(p => p.Definition.Name)
                .Concat(linkedParams.Select(p => p.Definition.Name))
                .Concat(associatedParams) // Параметры, связанные с вложенными семействами и геометрией
                .Concat(formulaReferencedParams) // Добавляем параметры, участвующие в формулах
            );

            // 7. Вычитаем параметры: оставляем только те, которых нет в excludedParamNames
            List<FamilyParameter> unusedParams = allParams
                .Where(p => !excludedParamNames.Contains(p.Definition.Name))
                .ToList();

            // 8. Открываем форму с результатами
            ParameterTableForm2 tableForm = new ParameterTableForm2(
                allParams, builtInParams, linkedParams, associatedParams, formulaReferencedParams, unusedParams);
            tableForm.ShowDialog();

            tx.Commit();
        }

        return Result.Succeeded;
    }

}

/// <summary>Форма с таблицами параметров.</summary>
public class ParameterTableForm2 : System.Windows.Forms.Form
{
    public ParameterTableForm2(
        List<FamilyParameter> allParams, List<FamilyParameter> builtInParams,
        List<FamilyParameter> linkedParams, List<string> associatedParams,
        List<string> formulaReferencedParams, List<FamilyParameter> unusedParams)
    {
        this.Text = "Анализ параметров семейства";
        this.Width = 1200;
        this.Height = 800;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new System.Drawing.Size(1000, 700);

        TableLayoutPanel layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 1,
            RowCount = 6
        };

        // Добавляем панели для каждой таблицы с параметрами
        layout.Controls.Add(CreateLabeledGridView("Все параметры семейства", allParams.Select(p => new[] { p.Definition.Name }).ToList()), 0, 0);
        layout.Controls.Add(CreateLabeledGridView("Встроенные параметры", builtInParams.Select(p => new[] { p.Definition.Name }).ToList()), 0, 1);
        layout.Controls.Add(CreateLabeledGridView("Параметры, связанные с размерами", linkedParams.Select(p => new[] { p.Definition.Name }).ToList()), 0, 2);
        layout.Controls.Add(CreateLabeledGridView("Параметры, связанные с вложенными семействами или геом. обьектом", associatedParams.Select(p => new[] { p }).ToList()), 0, 3);
        layout.Controls.Add(CreateLabeledGridView("Параметры, участвующие в формулах или имеют формулу", formulaReferencedParams.Select(p => new[] { p }).ToList()), 0, 4);
        layout.Controls.Add(CreateLabeledGridView("Ненужные параметры", unusedParams.Select(p => new[] { p.Definition.Name }).ToList()), 0, 5);

        this.Controls.Add(layout);
    }

    private WinFormsPanel CreateLabeledGridView(string title, List<string[]> data)
    {
        WinFormsPanel panel = new WinFormsPanel
        {
            Dock = DockStyle.Top,
            Padding = new Padding(5),
            BorderStyle = BorderStyle.FixedSingle,
            Width = 1150,
            Height = 300 // Фиксированная высота панели
        };

        Label label = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold),
            Height = 30
        };

        DataGridView gridView = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            Height = 270,
            MinimumSize = new System.Drawing.Size(1000, 270)
        };

        if (data.Count > 0)
        {
            for (int i = 0; i < data[0].Length; i++)
            {
                DataGridViewColumn col = new DataGridViewTextBoxColumn
                {
                    HeaderText = $"Колонка {i + 1}",
                    Name = $"Column{i}",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 100
                };
                gridView.Columns.Add(col);
            }

            foreach (var row in data)
                gridView.Rows.Add(row);
        }
        else
        {
            gridView.Columns.Add("NoData", "Нет данных");
        }

        panel.Controls.Add(label);
        panel.Controls.Add(gridView);
        return panel;
    }
}

