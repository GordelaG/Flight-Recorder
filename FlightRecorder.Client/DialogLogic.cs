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

        public string CurrentLanguage { get; set; } = "en";

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

            bool isPt = CurrentLanguage.Equals("pt-BR", StringComparison.OrdinalIgnoreCase) || CurrentLanguage.Equals("pt", StringComparison.OrdinalIgnoreCase);
            var options = new ConfirmDialogOptions
            {
                Title = isPt ? "Atenção" : "Attention",
                Message = error,
                ConfirmText = isPt ? "Entendi" : "OK",
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
            bool isPt = CurrentLanguage.Equals("pt-BR", StringComparison.OrdinalIgnoreCase) || CurrentLanguage.Equals("pt", StringComparison.OrdinalIgnoreCase);

            if (message.Contains("Starting a new recording will remove current one", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = isPt ? "Gravação Não Salva" : "Unsaved Recording",
                    Message = isPt
                        ? "Você possui um voo gravado que ainda não foi salvo.\nIniciar uma nova gravação substituirá o voo atual.\n\nDeseja continuar?"
                        : "You have an unsaved recorded flight.\nStarting a new recording will replace the current one.\n\nDo you want to proceed?",
                    ConfirmText = isPt ? "Descartar e Gravar" : "Discard and Record",
                    CancelText = isPt ? "Cancelar" : "Cancel",
                    Variant = DialogVariant.Warning
                };
            }

            if (message.Contains("Loading a recording will remove current one", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = isPt ? "Gravação Não Salva" : "Unsaved Recording",
                    Message = isPt
                        ? "Você possui um voo gravado que ainda não foi salvo.\nCarregar outra gravação substituirá o voo atual.\n\nDeseja continuar?"
                        : "You have an unsaved recorded flight.\nLoading another flight will replace the current one.\n\nDo you want to proceed?",
                    ConfirmText = isPt ? "Descartar e Carregar" : "Discard and Load",
                    CancelText = isPt ? "Cancelar" : "Cancel",
                    Variant = DialogVariant.Warning
                };
            }

            if (message.Contains("Trimming will remove all frames before", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = isPt ? "Cortar Início do Voo" : "Trim Flight Start",
                    Message = isPt
                        ? "Você possui um voo gravado não salvo.\nO corte removerá permanentemente todos os quadros anteriores ao ponto atual.\n\nDeseja continuar?"
                        : "You have an unsaved flight.\nTrimming will permanently remove all frames before the current point.\n\nDo you want to proceed?",
                    ConfirmText = isPt ? "Confirmar Corte" : "Confirm Trim",
                    CancelText = isPt ? "Cancelar" : "Cancel",
                    Variant = DialogVariant.Warning
                };
            }

            if (message.Contains("Trimming will remove all frames after", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = isPt ? "Cortar Fim do Voo" : "Trim Flight End",
                    Message = isPt
                        ? "Você possui um voo gravado não salvo.\nO corte removerá permanentemente todos os quadros posteriores ao ponto atual.\n\nDeseja continuar?"
                        : "You have an unsaved flight.\nTrimming will permanently remove all frames after the current point.\n\nDo you want to proceed?",
                    ConfirmText = isPt ? "Confirmar Corte" : "Confirm Trim",
                    CancelText = isPt ? "Cancelar" : "Cancel",
                    Variant = DialogVariant.Warning
                };
            }

            if (message.Contains("Do you want to exit Flight Recorder without saving", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = isPt ? "Sair sem Salvar" : "Exit Without Saving",
                    Message = isPt
                        ? "Você possui um voo gravado que ainda não foi salvo.\n\nDeseja realmente fechar o Flight Recorder sem salvar a gravação?"
                        : "You have an unsaved recorded flight.\n\nDo you really want to close Flight Recorder without saving?",
                    ConfirmText = isPt ? "Sair sem Salvar" : "Exit Without Saving",
                    CancelText = isPt ? "Voltar" : "Back",
                    Variant = DialogVariant.Danger
                };
            }

            if (message.Contains("crash was found", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = isPt ? "Recuperar Gravação de Voo" : "Recover Flight Recording",
                    Message = isPt
                        ? "Foi encontrado um salvamento automático recuperado de um fechamento anterior do Flight Recorder.\n\nDeseja restaurar esta gravação?"
                        : "An auto-save from a previous unexpected close of Flight Recorder was found.\n\nDo you want to restore this recording?",
                    ConfirmText = isPt ? "Restaurar Voo" : "Restore Flight",
                    CancelText = isPt ? "Descartar" : "Discard",
                    Variant = DialogVariant.Info
                };
            }

            return new ConfirmDialogOptions
            {
                Title = isPt ? "Confirmação" : "Confirmation",
                Message = message,
                ConfirmText = isPt ? "Sim" : "Yes",
                CancelText = isPt ? "Não" : "No",
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
