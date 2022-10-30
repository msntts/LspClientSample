using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;

namespace LspClientSample
{
    public partial class Form1 : Form
    {
        private Process? _lspServer;
        private LanguageClient? _client;

        private readonly string _clangdPath = @"C:\Program Files\LLVM\bin\clangd.exe";

        public Form1()
        {
            InitializeComponent();

            UpdateCodesCombobox();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _codeDirText.Text = dialog.SelectedPath;

                UpdateCodesCombobox();
            }
        }

        private void UpdateCodesCombobox()
        {
            _codesCombo.Items.Clear();

            // _codeDir�ȉ��ɂ���.c�t�@�C����T��
            if (!string.IsNullOrEmpty(_codeDirText.Text))
            {
                var files = Directory.EnumerateFiles(_codeDirText.Text, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.ToLower().EndsWith(".c") || f.ToLower().EndsWith(".h"));

                foreach (var file in files)
                {
                    // �t�@�C�����̓��[�g�t�H���_�ȉ���\��
                    _codesCombo.Items.Add(file.Replace(_codeDirText.Text + "\\", ""));
                }
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                // LSP Server�̏�����
                if (_lspServer is null)
                {
                    var startInfo = new ProcessStartInfo(_clangdPath,
                        @"--compile-commands-dir=" + _codeDirText.Text + " --log=verbose");

                    startInfo.RedirectStandardInput = true;
                    startInfo.RedirectStandardOutput = true;

                    _lspServer = Process.Start(startInfo);
                }

                // LSP Client�̏�����
                if ((_lspServer is not null) && (_client is null))
                {
                    var options = new LanguageClientOptions();
                    options.Input = PipeReader.Create(_lspServer.StandardOutput.BaseStream);
                    options.Output = PipeWriter.Create(_lspServer.StandardInput.BaseStream);
                    options.RootUri = DocumentUri.From(_codeDirText.Text);
                    options.WithCapability(
                        new DocumentSymbolCapability
                        {
                            DynamicRegistration = true,
                            SymbolKind = new SymbolKindCapabilityOptions
                            {
                                ValueSet = new Container<SymbolKind>(
                                    Enum.GetValues(typeof(SymbolKind)).Cast<SymbolKind>()
                                        .ToArray()
                                )
                            },
                            TagSupport = new TagSupportCapabilityOptions
                            {
                                ValueSet = new[] { SymbolTag.Deprecated }
                            },
                            HierarchicalDocumentSymbolSupport = true
                        }
                    );

                    _client = LanguageClient.Create(options);

                    var token = new CancellationToken();
                    await _client.Initialize(token);
                }

                if ((_lspServer is not null) && (_client is not null) && (_codesCombo.Text is not null))
                {
                    var path = $"{_codeDirText.Text}\\{_codesCombo.SelectedItem}";

                    // LSP�̃V�[�P���X�Ƃ��āA�t�@�C����Open���Ă���₢���킹��
                    // DidOpen�𑗂�ƃT�[�o�[���Ńe�L�X�g���J��
                    _client.DidOpenTextDocument(new DidOpenTextDocumentParams
                    {
                        TextDocument = new TextDocumentItem
                        {
                            LanguageId = "c", // �����͗ǂ��������ĂȂ��E�E�E��
                            Text = File.ReadAllText(path),
                            Uri = new Uri(path)
                        }
                    });

                    // �V���{���̎擾���N�G�X�g
                    var symbols = await _client.RequestDocumentSymbol(new DocumentSymbolParams
                    {
                        TextDocument = DocumentUri.From(path)
                    });

                    textBox1.Text = string.Empty;

                    textBox1.Text = ExtractSymbolsInfo(symbols);
                }
            }
            catch (Exception ex)
            {
                textBox1.Text = ex.Message;
            }
        }

        private string ExtractSymbolsInfo(SymbolInformationOrDocumentSymbolContainer symbols)
        {
            var sb = new StringBuilder();

            foreach (var symbol in symbols)
            {
                if (symbol.DocumentSymbol is not null)
                {
                    sb.Append(ExtractSymbolInfo(symbol.DocumentSymbol));
                }
            }

            return sb.ToString();
        }

        private string ExtractSymbolInfo(DocumentSymbol symbol, int depth = 0)
        {
            var sb = new StringBuilder();
            string indent = new string(' ', depth * 2);

            sb.AppendLine($"{indent}KIND: {symbol.Kind}");
            sb.AppendLine($"{indent}NAME: {symbol.Name}");
            sb.AppendLine($"{indent}DTIL: {symbol.Detail}");

            if (symbol.Children is not null)
            {
                foreach (var child in symbol.Children)
                {
                    sb.Append(ExtractSymbolInfo(child, ++depth));
                }
            }

            return sb.ToString();
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_lspServer is not null)
            {
                _lspServer.Dispose();
            }
        }
    }
}