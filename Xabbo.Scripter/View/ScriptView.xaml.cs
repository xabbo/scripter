using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

using RoslynPad.Editor;
using RoslynPad.Roslyn;

using Xabbo.Scripter.Scripting;
using Xabbo.Scripter.ViewModel;
using Xabbo.Scripter.Events;
using Xabbo.Scripter.Theme;
using System.Xml;

using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;

namespace Xabbo.Scripter.View
{
    public partial class ScriptView : UserControl
    {
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
            Unloaded += ScriptView_Unloaded;

            DataContextChanged += ScriptView_DataContextChanged;

            textBoxLog.TextChanged += TextBoxLog_TextChanged;
        }

        private void TextBoxLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxLog.ScrollToEnd();
        }

        private void ScriptView_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ScriptView_Unloaded");
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
            Debug.WriteLine("ScriptView_Loaded");

            if (DataContext is ScriptViewModel viewModel)
            {
                InitializeFromViewModel(viewModel);
            }

            /*Script.CompileError += Script_CompileError;
            Script.RuntimeError += Script_RuntimeError;
            Script.Progress.ProgressChanged += Progress_ProgressChanged;*/
        }

        private void Script_RuntimeError(object? sender, RuntimeErrorEventArgs e)
        {
#if FALSE
            var sourceLocations = e.Error.GetSourceLocations();

            var location = sourceLocations.Where(loc => !string.IsNullOrWhiteSpace(loc.Path) && Path.GetFileName(loc.Path) == "script.csx").FirstOrDefault();

            if (location.Path != null)
            {
                // codeEditor.Select(lastLocation.Span.Start., lastLocation.Span.End);
                (int start, int length) = Script.Code.GetSpan(location.Span.Start.Line);
                if (start >= 0)
                {
                    SelectSpan(
                        Location.Create(
                            location.Path,
                            new TextSpan(start, length),
                            location.Span
                        )
                    );

                    /*codeEditor.Focus();
                    codeEditor.ScrollTo(location.StartLinePosition.Line, location.StartLinePosition.Character);
                    codeEditor.Select(index, length);*/
                }
            }
#endif
        }

        private void Script_CompileError(object? sender, CompileErrorEventArgs e)
        {
            if (e.Error is not CompilationErrorException ex) return;

            var diagnostic = ex.Diagnostics.FirstOrDefault();

            if (diagnostic != null)
            {
                SyntaxTree? syntaxTree = diagnostic.Location.SourceTree;
                if (syntaxTree is null) return;
                string sourcePath = syntaxTree.FilePath
                    .Replace("/", Path.DirectorySeparatorChar.ToString());
                string sourcePathName = sourcePath; // PathUtil.GetRelativePath(sourcePath, Path.GetFullPath("scripts"));

                var lineSpan = diagnostic.Location.GetLineSpan();
                var linePosition = diagnostic.Location.GetLineSpan().StartLinePosition;

                //if (sourcePath.Equals(scriptFilePath, StringComparison.InvariantCultureIgnoreCase))
                {
                    sourcePathName = "<script>";

                    codeEditor.Focus();
                    codeEditor.ScrollTo(linePosition.Line, linePosition.Character);
                    codeEditor.Select(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
                }

                //Script.LogText = $"compile error: {diagnostic.GetMessage()}";
                //Script.LogText += $"\n  at {sourcePathName}:{lineSpan.StartLinePosition}";
            }
            else
            {
                //Script.LogText = ex.ToString();
            }
        }

        private void Progress_ProgressChanged(object? sender, ScriptUpdate e) => ScrollToEnd();

        private void ScrollToEnd()
        {
            if (!CheckAccess())
            {
                Dispatcher.InvokeAsync(() => ScrollToEnd());
                return;
            }

            // textBoxLog.Focus();
            //textBoxLog.CaretIndex = Script.LogText.Length;
            textBoxLog.ScrollToEnd();
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

        private void ButtonExecute_Click(object sender, RoutedEventArgs e)
        {
            /*if (Script.IsRunning)
            {
                if (Script.IsCancelling) return;
                Script.Cancel();
                return;
            }

            RunScript();*/

            /*try
            {
                //await Window.ExecuteScriptAsync(Script);
            }
            catch (Exception ex)
            {
                if (ex is ScripterRuntimeException runtimeEx)
                    ex = runtimeEx.InnerException;

                var sb = new StringBuilder("error: ");
                sb.Append(ex.Message);

                var (line, col) = ex.GetSourceLocation(Script.Path);
                if (line != -1)
                {
                    codeEditor.Focus();
                    codeEditor.ScrollTo(line, col);

                    (int index, int len) = Script.Code.GetSpan(line);
                    if (index != -1) { codeEditor.Select(index, len); }
                }

                foreach (var location in ex.GetSourceLocations())
                {
                    sb.Append($"\n  at ");

                    if (location.Path == Script.Path)
                        sb.Append("<script>.csx");
                    else
                    {
                        string relativePath = PathUtil.GetRelativePath(location.Path, Path.GetFullPath("scripts"));
                        sb.Append(relativePath);
                    }

                    sb.Append($":{location.StartLinePosition.Line},{location.StartLinePosition.Character}");
                }

                Script.Status = ScriptStatus.RuntimeError;
                Script.IsFaulted = true;
                Script.LogText = ex.ToString();
            }*/
        }

        private void RunScript()
        {
            /*Script.Code = codeEditor.Text;
            Script.Path = @"scripts\script.csx"; // TODO

            ScriptEngine.RunScript(Script, Window.AbortToken);*/
        }

        private async Task ExecuteScriptAsync()
        {
#if FALSE
            throw new NotImplementedException();

            string scriptFileName = Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".csx";
            string scriptFilePath = Path.GetFullPath(Path.Combine("temp", scriptFileName));

            Script.Code = codeEditor.Text;

            bool wasFinished = false;

            try
            {
                Script.IsRunning = true;
                Script.IsFaulted = false;
                codeEditor.IsReadOnly = true;

                isLogging = false;

                Script.CancellationSource = CancellationTokenSource.CreateLinkedTokenSource(Window.ShutdownToken);
                var token = Script.CancellationSource.Token;

                File.WriteAllText(scriptFilePath, Script.Code, Encoding.UTF8);

                Script.Status = ScriptStatus.Executing;

                object result = null;
                try
                {
                    using (var g = new G(null, Window.ScriptManager, Script))
                    {
                        try
                        {
                            result = await Task.Run(() => Script.Runner(globals: g, cancellationToken: token));
                        }
                        finally { wasFinished = g.IsFinished; }
                    }
                }
                catch (Exception ex)
                {
                    bool isScriptError = ex is ScriptException;

                    var sb = new StringBuilder("error: ");

                    Exception originalException = ex;
                    if (ex is AggregateException)
                        ex = ex.InnerException;

                    ex.PrintStackTrace();

                    /*var (line, col) = ex.GetSourceLocation(scriptFilePath);
                    if (line != -1)
                    {
                        codeEditor.Focus();
                        codeEditor.ScrollTo(line, col);

                        (int index, int len) = Script.Code.GetSpan(line);
                        if (index != -1) { codeEditor.Select(index, len); }
                    }*/

                    if (ex is OperationCanceledException) throw ex;

                    /*if (ex is ListenerAttachFailedException attachFailedException)
                    {
                        //var unresolved = MainWindow.Module.Dispatcher.Headers.GetUnresolvedIdentifiers(attachFailedException.Listener);

                        sb.Append("required headers are missing; ");
                        //sb.Append(unresolved.ToString());
                    }
                    else*/
                    {
                        sb.Append(ex.Message);
                    }

                    /*foreach (var location in ex.GetSourceLocations())
                    {
                        sb.Append($"\n  at ");

                        if (location.Path == scriptFilePath)
                            sb.Append("<script>.csx");
                        else
                        {
                            string relativePath = PathUtil.GetRelativePath(location.Path, Path.GetFullPath("scripts"));
                            sb.Append(relativePath);
                        }

                        sb.Append($":{location.StartLinePosition.Line},{location.StartLinePosition.Character}");
                    }*/

                    Script.Status = ScriptStatus.RuntimeError;
                    /*Script.IsFaulted = true;
                    Script.LogText = sb.ToString();*/
                    return;
                }

                string text;
                if (Script.CancellationSource.IsCancellationRequested)
                {
                    Script.Status = ScriptStatus.Canceled;
                    text = "Execution canceled.";
                }
                else
                {
                    Script.Status = ScriptStatus.Complete;
                    text = $"Execution complete.";
                    if (result != null) text += $" Result: {result}";
                }

                if (isLogging)
                {
                    ScrollToEnd();
                }
                else
                    Script.LogText = text;
            }
            catch (CompilationErrorException ex)
            {
                Script.Status = ScriptStatus.CompileError;
                Script.IsFaulted = true;

                var diagnostic = ex.Diagnostics.FirstOrDefault();

                if (diagnostic != null)
                {
                    string sourcePath = diagnostic.Location.SourceTree.FilePath
                        .Replace("/", Path.DirectorySeparatorChar.ToString());
                    string sourcePathName = PathUtil.GetRelativePath(sourcePath, Path.GetFullPath("scripts"));

                    var lineSpan = diagnostic.Location.GetLineSpan();
                    var linePosition = diagnostic.Location.GetLineSpan().StartLinePosition;

                    if (sourcePath.Equals(scriptFilePath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        sourcePathName = "<script>";

                        codeEditor.Focus();
                        codeEditor.ScrollTo(linePosition.Line, linePosition.Character);
                        codeEditor.Select(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
                    }

                    Script.LogText = $"compile error: {diagnostic.GetMessage()}";
                    Script.LogText += $"\n  at {sourcePathName}:{lineSpan.StartLinePosition}";
                }
                else
                {
                    Script.LogText = ex.ToString();
                }
            }
            catch (OperationCanceledException)
            {
                if (Window.ShutdownToken.IsCancellationRequested)
                {
                    Script.LogText = "Shutting down...";
                }
                else
                {
                    string message;

                    if (wasFinished)
                    {
                        Script.Status = ScriptStatus.Complete;
                        message = "Execution was finished by the script.";
                    }
                    else if (!Script.CancellationSource.IsCancellationRequested)
                    {
                        Script.Status = ScriptStatus.TimedOut;
                        message = "Execution timed out.";
                    }
                    else
                    {
                        Script.Status = ScriptStatus.Canceled;
                        message = "Execution canceled.";
                    }

                    if (isLogging) ScrollToEnd();
                    else Script.LogText = message;
                }
            }
            catch (Exception ex)
            {
                Script.Status = ScriptStatus.UnknownError;
                Script.IsFaulted = true;

                if (ex.InnerException != null) ex = ex.InnerException;
                Debug.WriteLine($"[b7scripter] An unexpected error occurred: {ex.Message}\r\n{ex.StackTrace}");

                Script.LogText = $"An unexpected error occurred: {ex.Message}";
            }
            finally
            {
                Script.CancellationSource.Cancel();
                Script.CancellationSource.Dispose();
                Script.CancellationSource = null;

                Script.IsRunning = false;
                Script.IsCancelling = false;
                Script.CanExecute = true;
                codeEditor.IsReadOnly = false;

                try { File.Delete(scriptFilePath); }
                catch { }
            }
#endif
        }

        private void CodeEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                e.Handled = true;
                Script.ExecuteCommand.Execute(null);
            }
        }

        private void CodeEditor_TextChanged(object sender, EventArgs e)
        {
            /*if (Script.LastSavedCodeLength != codeEditor.Text.Length ||
                Script.LastSavedCodeHash != ScriptUtil.ComputeHash(codeEditor.Text))
            {
                Script.IsCodeModified = true;
            }
            else
            {
                Script.IsCodeModified = false;
            }*/

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

        private bool _isInitialized;

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
                string.Empty
            );
            codeEditor.TextArea.LeftMargins.RemoveAt(1);

            _isInitialized = true;
        }

        private void CodeEditor_CreatingDocument(object? sender, CreatingDocumentEventArgs e)
        {
            e.DocumentId = Script.Engine.RoslynHost.AddDocument(
                //Script.Engine.BaseDocumentId,
                new DocumentCreationArgs(
                    e.TextContainer,
                    Path.GetFullPath(Script.Engine.ScriptDirectory),
                    e.ProcessDiagnostics
                )
            );
        }
    }
}
