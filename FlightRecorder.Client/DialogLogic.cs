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

            string lang = CurrentLanguage?.ToLowerInvariant() ?? "en";
            bool isPt = lang.StartsWith("pt");
            bool isEs = lang.StartsWith("es");
            bool isFr = lang.StartsWith("fr");

            var options = new ConfirmDialogOptions
            {
                Title = isPt ? "Atenção" : isEs ? "Atención" : isFr ? "Attention" : "Attention",
                Message = error,
                ConfirmText = isPt ? "Entendi" : isEs ? "Entendido" : isFr ? "Compris" : "OK",
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
            string lang = CurrentLanguage?.ToLowerInvariant() ?? "en";
            bool isPt = lang.StartsWith("pt");
            bool isEs = lang.StartsWith("es");
            bool isFr = lang.StartsWith("fr");

            if (message.Contains("Starting a new recording will remove current one", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = isPt ? "Gravação Não Salva" : isEs ? "Grabación No Guardada" : isFr ? "Enregistrement Non Sauvegardé" : "Unsaved Recording",
                    Message = isPt
                        ? "Você possui um voo gravado que ainda não foi salvo.\nIniciar uma nova gravação substituirá o voo atual.\n\nDeseja continuar?"
                        : isEs
                        ? "Tienes un vuelo grabado que aún no se ha guardado.\nIniciar una nueva grabación reemplazará el vuelo actual.\n\n¿Deseas continuar?"
                        : isFr
                        ? "Vous avez un vol enregistré qui n'est pas encore sauvegardé.\nDémarrer un nouvel enregistrement remplacera le vol actuel.\n\nSouhaitez-vous continuer ?"
                        : "You have an unsaved recorded flight.\nStarting a new recording will replace the current one.\n\nDo you want to proceed?",
                    ConfirmText = isPt ? "Descartar e Gravar" : isEs ? "Descartar y Grabar" : isFr ? "Ignorer et Enregistrer" : "Discard and Record",
                    CancelText = isPt ? "Cancelar" : isEs ? "Cancelar" : isFr ? "Annuler" : "Cancel",
                    Variant = DialogVariant.Warning
                };
            }

            if (message.Contains("Loading a recording will remove current one", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = isPt ? "Gravação Não Salva" : isEs ? "Grabación No Guardada" : isFr ? "Enregistrement Non Sauvegardé" : "Unsaved Recording",
                    Message = isPt
                        ? "Você possui um voo gravado que ainda não foi salvo.\nCarregar outra gravação substituirá o voo atual.\n\nDeseja continuar?"
                        : isEs
                        ? "Tienes un vuelo grabado que aún no se ha guardado.\nCargar otra grabación reemplazará el vuelo actual.\n\n¿Deseas continuar?"
                        : isFr
                        ? "Vous avez un vol enregistré qui n'est pas encore sauvegardé.\nCharger un autre enregistrement remplacera le vol actuel.\n\nSouhaitez-vous continuer ?"
                        : "You have an unsaved recorded flight.\nLoading another flight will replace the current one.\n\nDo you want to proceed?",
                    ConfirmText = isPt ? "Descartar e Carregar" : isEs ? "Descartar y Cargar" : isFr ? "Ignorer et Charger" : "Discard and Load",
                    CancelText = isPt ? "Cancelar" : isEs ? "Cancelar" : isFr ? "Annuler" : "Cancel",
                    Variant = DialogVariant.Warning
                };
            }

            if (message.Contains("Trimming will remove all frames before", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = isPt ? "Cortar Início do Voo" : isEs ? "Recortar Inicio del Vuelo" : isFr ? "Rogner le Début du Vol" : "Trim Flight Start",
                    Message = isPt
                        ? "Você possui um voo gravado não salvo.\nO corte removerá permanentemente todos os quadros anteriores ao ponto atual.\n\nDeseja continuar?"
                        : isEs
                        ? "Tienes un vuelo grabado no guardado.\nEl recorte eliminará permanentemente todos los fotogramas anteriores al punto actual.\n\n¿Deseas continuar?"
                        : isFr
                        ? "Vous avez un vol enregistré non sauvegardé.\nLe rognage supprimera définitivement toutes les images précédant le point actuel.\n\nSouhaitez-vous continuer ?"
                        : "You have an unsaved flight.\nTrimming will permanently remove all frames before the current point.\n\nDo you want to proceed?",
                    ConfirmText = isPt ? "Confirmar Corte" : isEs ? "Confirmar Recorte" : isFr ? "Confirmer le Rognage" : "Confirm Trim",
                    CancelText = isPt ? "Cancelar" : isEs ? "Cancelar" : isFr ? "Annuler" : "Cancel",
                    Variant = DialogVariant.Warning
                };
            }

            if (message.Contains("Trimming will remove all frames after", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = isPt ? "Cortar Fim do Voo" : isEs ? "Recortar Final del Vuelo" : isFr ? "Rogner la Fin du Vol" : "Trim Flight End",
                    Message = isPt
                        ? "Você possui um voo gravado não salvo.\nO corte removerá permanentemente todos os quadros posteriores ao ponto atual.\n\nDeseja continuar?"
                        : isEs
                        ? "Tienes un vuelo grabado no guardado.\nEl recorte eliminará permanentemente todos los fotogramas posteriores al punto actual.\n\n¿Deseas continuar?"
                        : isFr
                        ? "Vous avez un vol enregistré non sauvegardé.\nLe rognage supprimera définitivement toutes les images suivant le point actuel.\n\nSouhaitez-vous continuer ?"
                        : "You have an unsaved flight.\nTrimming will permanently remove all frames after the current point.\n\nDo you want to proceed?",
                    ConfirmText = isPt ? "Confirmar Corte" : isEs ? "Confirmar Recorte" : isFr ? "Confirmer le Rognage" : "Confirm Trim",
                    CancelText = isPt ? "Cancelar" : isEs ? "Cancelar" : isFr ? "Annuler" : "Cancel",
                    Variant = DialogVariant.Warning
                };
            }

            if (message.Contains("Do you want to exit Flight Recorder without saving", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = isPt ? "Sair sem Salvar" : isEs ? "Salir sin Guardar" : isFr ? "Quitter sans Sauvegarder" : "Exit Without Saving",
                    Message = isPt
                        ? "Você possui um voo gravado que ainda não foi salvo.\n\nDeseja realmente fechar o Flight Recorder sem salvar a gravação?"
                        : isEs
                        ? "Tienes un vuelo grabado que aún no se ha guardado.\n\n¿Realmente deseas cerrar Flight Recorder sin guardar?"
                        : isFr
                        ? "Vous avez un vol enregistré qui n'est pas encore sauvegardé.\n\nVoulez-vous vraiment fermer Flight Recorder sans sauvegarder ?"
                        : "You have an unsaved recorded flight.\n\nDo you really want to close Flight Recorder without saving?",
                    ConfirmText = isPt ? "Sair sem Salvar" : isEs ? "Salir sin Guardar" : isFr ? "Quitter sans Sauvegarder" : "Exit Without Saving",
                    CancelText = isPt ? "Voltar" : isEs ? "Volver" : isFr ? "Retour" : "Back",
                    Variant = DialogVariant.Danger
                };
            }

            if (message.Contains("crash was found", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfirmDialogOptions
                {
                    Title = isPt ? "Recuperar Gravação de Voo" : isEs ? "Recuperar Grabación de Vuelo" : isFr ? "Récupérer le Vol Enregistré" : "Recover Flight Recording",
                    Message = isPt
                        ? "Foi encontrado um salvamento automático recuperado de um fechamento anterior do Flight Recorder.\n\nDeseja restaurar esta gravação?"
                        : isEs
                        ? "Se encontró un autoguardado de un cierre anterior inesperado de Flight Recorder.\n\n¿Deseas restaurar esta grabación?"
                        : isFr
                        ? "Une sauvegarde automatique issue d'une fermeture inattendue précédente de Flight Recorder a été trouvée.\n\nSouhaitez-vous restaurer cet enregistrement ?"
                        : "An auto-save from a previous unexpected close of Flight Recorder was found.\n\nDo you want to restore this recording?",
                    ConfirmText = isPt ? "Restaurar Voo" : isEs ? "Restaurar Vuelo" : isFr ? "Restaurer le Vol" : "Restore Flight",
                    CancelText = isPt ? "Descartar" : isEs ? "Descartar" : isFr ? "Ignorer" : "Discard",
                    Variant = DialogVariant.Info
                };
            }

            return new ConfirmDialogOptions
            {
                Title = isPt ? "Confirmação" : isEs ? "Confirmación" : isFr ? "Confirmation" : "Confirmation",
                Message = message,
                ConfirmText = isPt ? "Sim" : isEs ? "Sí" : isFr ? "Oui" : "Yes",
                CancelText = isPt ? "Não" : isEs ? "No" : isFr ? "Non" : "No",
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
