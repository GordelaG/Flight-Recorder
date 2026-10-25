using FlightRecorder.Client.Logics;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace FlightRecorder.Client
{
    public class DialogLogic : IDialogLogic
    {
        private const string recorderFileFilter = "Recorded Flight|*.fltrec;*.flightrecorder";

        private readonly ILogger<DialogLogic> logger;

        public DialogLogic(ILogger<DialogLogic> logger)
        {
            this.logger = logger;
        }

        public bool Confirm(string message)
        {
            if (System.Windows.Application.Current?.Dispatcher != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                return System.Windows.Application.Current.Dispatcher.Invoke(() => Confirm(message));
            }

            var options = ParseConfirmOptions(message);
            var dialog = new FluxConfirmDialog(options);
            SetDialogOwner(dialog);

            return dialog.ShowDialog() == true;
        }

        public void Error(string error)
        {
            if (System.Windows.Application.Current?.Dispatcher != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => Error(error));
                return;
            }

            var options = new ConfirmDialogOptions
            {
                Title = "Atenção",
                Message = error,
                ConfirmText = "Entendi",
                CancelText = null,
                Variant = DialogVariant.Danger
            };

            var dialog = new FluxConfirmDialog(options);
            SetDialogOwner(dialog);
            dialog.ShowDialog();
        }

        private void SetDialogOwner(Window dialog)
        {
            try
            {
                var activeWindow = System.Windows.Application.Current?.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w.IsActive && w.IsVisible && w != dialog)
                    ?? System.Windows.Application.Current?.MainWindow;

                if (activeWindow != null && activeWindow.IsVisible)
                {
                    dialog.Owner = activeWindow;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    dialog.Topmost = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to set dialog owner.");
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.Topmost = true;
            }
        }

        private ConfirmDialogOptions ParseConfirmOptions(string message)
        {
            if (message.Contains("Starting a new recording will remove current one", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = "Gravação Não Salva",
                    Message = "Você possui um voo gravado que ainda não foi salvo.\nIniciar uma nova gravação substituirá o voo atual.\n\nDeseja continuar?",
                    ConfirmText = "Descartar e Gravar",
                    CancelText = "Cancelar",
                    Variant = DialogVariant.Warning
                };
            }

            if (message.Contains("Loading a recording will remove current one", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = "Gravação Não Salva",
                    Message = "Você possui um voo gravado que ainda não foi salvo.\nCarregar outra gravação substituirá o voo atual.\n\nDeseja continuar?",
                    ConfirmText = "Descartar e Carregar",
                    CancelText = "Cancelar",
                    Variant = DialogVariant.Warning
                };
            }

            if (message.Contains("Trimming will remove all frames before", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = "Cortar Início do Voo",
                    Message = "Você possui um voo gravado não salvo.\nO corte removerá permanentemente todos os quadros anteriores ao ponto atual.\n\nDeseja continuar?",
                    ConfirmText = "Confirmar Corte",
                    CancelText = "Cancelar",
                    Variant = DialogVariant.Warning
                };
            }

            if (message.Contains("Trimming will remove all frames after", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = "Cortar Fim do Voo",
                    Message = "Você possui um voo gravado não salvo.\nO corte removerá permanentemente todos os quadros posteriores ao ponto atual.\n\nDeseja continuar?",
                    ConfirmText = "Confirmar Corte",
                    CancelText = "Cancelar",
                    Variant = DialogVariant.Warning
                };
            }

            if (message.Contains("Do you want to exit Flight Recorder without saving", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = "Sair sem Salvar",
                    Message = "Você possui um voo gravado que ainda não foi salvo.\n\nDeseja realmente fechar o Flight Recorder sem salvar a gravação?",
                    ConfirmText = "Sair sem Salvar",
                    CancelText = "Voltar",
                    Variant = DialogVariant.Danger
                };
            }

            if (message.Contains("crash was found", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = "Recuperar Gravação de Voo",
                    Message = "Foi encontrado um salvamento automático recuperado de um fechamento anterior do Flight Recorder.\n\nDeseja restaurar esta gravação?",
                    ConfirmText = "Restaurar Voo",
                    CancelText = "Descartar",
                    Variant = DialogVariant.Info
                };
            }

            return new ConfirmDialogOptions
            {
                Title = "Confirmação",
                Message = message,
                ConfirmText = "Sim",
                CancelText = "Não",
                Variant = DialogVariant.Warning
            };
        }

        /// <returns>Full path of the selected file or null</returns>
        public Task<string?> PickSaveFileAsync()
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"{DateTime.Now:yyyy-MM-dd-HH-mm}.fltrec",
                Filter = recorderFileFilter
            };
            if (dialog.ShowDialog() == true)
            {
                return Task.FromResult<string?>(dialog.FileName);
            }

            return Task.FromResult<string?>(null);
        }

        public async Task<(string filePath, Stream fileStream)?> PickOpenFileAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = recorderFileFilter
            };

            if (dialog.ShowDialog() == true)
            {
                return (dialog.FileName, dialog.OpenFile());
            }
            return null;
        }

        public Task<string?> PickSaveFolderAsync()
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return Task.FromResult<string?>(dialog.SelectedPath);
            }
            return Task.FromResult<string?>(null);
        }
    }
}
