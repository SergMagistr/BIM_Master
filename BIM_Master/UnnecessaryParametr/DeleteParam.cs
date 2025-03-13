using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;


using WinFormsPanel = System.Windows.Forms.Panel;


namespace DeleteParam
{

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
            ParameterTableForm tableForm = new ParameterTableForm(doc, unusedParams);
            tableForm.ShowDialog();

            return Result.Succeeded;
        }
    }

    public class ParameterTableForm : System.Windows.Forms.Form
    {
        private Document doc;
        private List<FamilyParameter> unusedParams;

        public ParameterTableForm(Document doc, List<FamilyParameter> unusedParams)
        {
            this.doc = doc;
            this.unusedParams = unusedParams;

            this.Text = "Ненужные параметры семейства";
            this.Width = 400;  // Ширина окна
            this.Height = 600; // Высота окна
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new System.Drawing.Size(300, 300);  // Ограничиваем минимальные размеры
            this.BackColor = System.Drawing.Color.WhiteSmoke;  // Цвет фона формы

            // Создаем контейнер для размещения элементов
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)  // Добавляем отступы
            };

            // Добавляем строку с таблицей, которая будет растягиваться
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 100% пространства для таблицы

            // Добавляем строку для кнопки с фиксированной высотой
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Строка с кнопкой

            // Добавляем таблицу для отображения ненужных параметров
            layout.Controls.Add(CreateGridView(unusedParams.Select(p => new[] { p.Definition.Name }).ToList()), 0, 0);

            // Добавляем кнопку "Удалить все" и выравниваем ее справа
            AddDeleteButton(layout);

            this.Controls.Add(layout);
        }

        private System.Windows.Forms.Panel CreateGridView(List<string[]> data)
        {
            System.Windows.Forms.Panel panel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle,
                Width = 300,
                BackColor = System.Drawing.Color.LightGray  // Цвет фона панели с таблицей
            };

            DataGridView gridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                Height = 570,
                MinimumSize = new System.Drawing.Size(300, 300),
                BackgroundColor = System.Drawing.Color.White,  // Цвет фона таблицы
                GridColor = System.Drawing.Color.LightGray,  // Цвет линий сетки
                ForeColor = System.Drawing.Color.Black,      // Цвет текста
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, // Выделение строки
                AllowUserToAddRows = false, // Запрещаем добавление строк
                AllowUserToDeleteRows = false  // Запрещаем удаление строк
            };

            if (data.Count > 0)
            {
                for (int i = 0; i < data[0].Length; i++)
                {
                    DataGridViewColumn col = new DataGridViewTextBoxColumn
                    {
                        HeaderText = $"Параметр",
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
                gridView.Columns.Add("NoData", "Нету ненужных параметров");
            }

            panel.Controls.Add(gridView);
            return panel;
        }

        private void AddDeleteButton(TableLayoutPanel layout)
        {
            Button deleteButton = new Button
            {
                Text = "Удалить все",
                Width = 150,
                Height = 30,
                BackColor = System.Drawing.Color.DarkSlateGray,  // Цвет фона кнопки
                ForeColor = System.Drawing.Color.White,          // Цвет текста кнопки
                Font = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Bold),  // Шрифт кнопки
                FlatStyle = FlatStyle.Flat,  // Стиль кнопки
                Cursor = Cursors.Hand  // Курсор в виде руки при наведении
            };

            // Изменение фона кнопки при наведении
            deleteButton.MouseEnter += (sender, e) => deleteButton.BackColor = System.Drawing.Color.DimGray;
            deleteButton.MouseLeave += (sender, e) => deleteButton.BackColor = System.Drawing.Color.DarkSlateGray;

            deleteButton.Click += (sender, e) => DeleteUnusedParameters();

            // Размещаем кнопку внизу справа
            deleteButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            layout.Controls.Add(deleteButton, 0, 1); // Убедитесь, что кнопка в правильной строке
        }

        private void DeleteUnusedParameters()
        {
            if (unusedParams == null || unusedParams.Count == 0)
            {
                MessageBox.Show("Нет ненужных параметров для удаления.");
                return;
            }

            using (Transaction tx = new Transaction(doc, "Удаление ненужных параметров"))
            {
                tx.Start();

                foreach (var param in unusedParams)
                {
                    try
                    {
                        if (param != null && !param.IsShared)
                        {
                            doc.FamilyManager.RemoveParameter(param);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении параметра {param.Definition.Name}: {ex.Message}");
                    }
                }

                tx.Commit();
            }

            MessageBox.Show("Ненужные параметры успешно удалены.");
            this.Close(); // Закрыть форму после отображения сообщения
        }
    }

}


