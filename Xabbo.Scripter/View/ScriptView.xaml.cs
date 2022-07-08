using System;
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;

using Microsoft.CodeAnalysis;

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

using RoslynPad.Editor;
using RoslynPad.Roslyn;

using Xabbo.Scripter.ViewModel;
using Xabbo.Scripter.Events;

namespace Xabbo.Scripter.View
{
    public partial class ScriptView : UserControl
    {
        private bool _isInitialized;

        public ScriptViewModel Script => (ScriptViewModel)DataContext;

        public ScriptView()
        {
            InitializeComponent();

            codeEditor.Options.AllowScrollBelowDocument = true;
            codeEditor.Options.ConvertTabsToSpaces = true;
            codeEditor.Options.IndentationSize = 2;
            codeEditor.CreatingDocument += CodeEditor_CreatingDocument;
            codeEditor.TextArea.Margin = new Thickness(8);

            Loaded += ScriptView_Loaded;

            DataContextChanged += ScriptView_DataContextChanged;

            textBoxLog.TextChanged += TextBoxLog_TextChanged;
        }

        private void TextBoxLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxLog.ScrollToEnd();
        }

        private void InitializeFromViewModel(ScriptViewModel viewModel)
        {
            codeEditor.Text = viewModel.Code;
            codeEditor.RefreshHighlighting();
        }

        private void ScriptView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ScriptViewModel oldViewModel)
            {
                oldViewModel.CompileError -= Script_CompileError;
                oldViewModel.RuntimeError -= Script_RuntimeError;

            }

            if (_isInitialized && e.NewValue is ScriptViewModel viewModel)
            {
                InitializeFromViewModel(viewModel);
            }
        }

        private void ScriptView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ScriptViewModel viewModel)
            {
                InitializeFromViewModel(viewModel);
            }
        }

        private void Script_RuntimeError(object? sender, RuntimeErrorEventArgs e)
        {
            // TODO: Highlight source line where error occurred.
        }

        private void Script_CompileError(object? sender, CompileErrorEventArgs e)
        {
            // TODO: Highlight source line where error occurred.
        }

        private void SelectSpan(Location location)
        {
            if (!CheckAccess())
            {
                Dispatcher.InvokeAsync(() => SelectSpan(location));
                return;
            }

            var linePosition = location.GetLineSpan().StartLinePosition;

            codeEditor.Focus();
            codeEditor.ScrollTo(linePosition.Line, linePosition.Character);
            codeEditor.Select(location.SourceSpan.Start, location.SourceSpan.Length);
        }

        private void CodeEditor_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= CodeEditor_Loaded;

            using (Stream s = File.OpenRead("theme.xshd"))
            {
                using XmlTextReader reader = new XmlTextReader(s);
                codeEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }

            codeEditor.Initialize(
                Script.Engine.RoslynHost,
                new ClassificationHighlightColors(),
                Path.GetFullPath("Scripts"),
                string.Empty,
                SourceCodeKind.Script
            );
            codeEditor.TextArea.LeftMargins.RemoveAt(1);

            _isInitialized = true;
        }

        private void CodeEditor_CreatingDocument(object? sender, CreatingDocumentEventArgs e)
        {
            e.DocumentId = Script.Engine.RoslynHost.AddDocument(
                new DocumentCreationArgs(
                    e.TextContainer,
                    Path.GetFullPath(Script.Engine.ScriptDirectory),
                    SourceCodeKind.Script,
                    e.ProcessDiagnostics
                )
            );
        }

        private void CodeEditor_TextChanged(object sender, EventArgs e)
        {
            Script.Code = codeEditor.Text;
        }

        private void CodeEditor_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double newSize = codeEditor.FontSize + (e.Delta > 0 ? 1 : -1);
                if (newSize < 11) newSize = 11;

                codeEditor.FontSize = newSize;
                e.Handled = true;
            }
        }

        private void CodeEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                e.Handled = true;
                Script.ExecuteCommand.Execute(null);
            }
        }
    }
}
