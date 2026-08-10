// <copyright file="HelpViewModel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using System.ComponentModel;
    using System.Windows.Input;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.DependencyInjection;
    using CommunityToolkit.Mvvm.Input;

    using FileConverter.Services;

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// </summary>
    public class HelpViewModel : ObservableRecipient
    {
        private RelayCommand<CancelEventArgs> closeCommand;

        /// <summary>
        /// Initializes a new instance of the HelpViewModel class.
        /// </summary>
        public HelpViewModel()
        {
        }

        public ICommand CloseCommand
        {
            get
            {
                if (this.closeCommand == null)
                {
                    this.closeCommand = new RelayCommand<CancelEventArgs>(this.Close);
                }

                return this.closeCommand;
            }
        }

        private void Close(CancelEventArgs args)
        {
            INavigationService navigationService = Ioc.Default.GetRequiredService<INavigationService>();

            Application application = Application.Current as Application;
            if (application?.ConsumeShowSettingsAfterHelpRequest() == true)
            {
                // Show the next main window before closing Help so the navigation service
                // does not interpret this transition as the end of the application.
                navigationService.Show(Pages.Settings);
            }

            navigationService.Close(Pages.Help, args != null);
        }
    }
}
