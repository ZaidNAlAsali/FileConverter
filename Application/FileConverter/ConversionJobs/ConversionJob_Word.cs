// <copyright file="ConversionJob_Word.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using FileConverter.Diagnostics;

    using Word = NetOffice.WordApi;

    public class ConversionJob_Word : ConversionJob_Office
    {
        private Word.Document document;
        private Word.Application application;

        private string intermediateFilePath = string.Empty;
        private ConversionJob pdf2ImageConversionJob = null;

        public ConversionJob_Word() : base()
        {
        }

        public ConversionJob_Word(ConversionPreset conversionPreset, string inputFilePath) : base(conversionPreset, inputFilePath)
        {
        }

        protected override ApplicationName Application => ApplicationName.Word;

        protected override bool IsCancelable() => false;

        protected override int GetOutputFilesCount()
        {
            if (this.ConversionPreset.OutputType == OutputType.Pdf)
            {
                return 1;
            }

            if (!this.TryLoadDocumentIfNecessary())
            {
                return 1;
            }

            int pagesCount = this.document.ComputeStatistics(Word.Enums.WdStatistic.wdStatisticPages);

            return pagesCount;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (this.State == ConversionState.Failed)
            {
                return;
            }

            if (this.ConversionPreset == null)
            {
                throw new System.Exception("The conversion preset must be valid.");
            }

            // Initialize converters.
            if (this.ConversionPreset.OutputType == OutputType.Pdf)
            {
                this.intermediateFilePath = this.OutputFilePath;
            }
            else
            {
                // Generate intermediate file path.
                string fileName = Path.GetFileNameWithoutExtension(this.InputFilePath);
                string tempPath = Path.GetTempPath();
                this.intermediateFilePath = PathHelpers.GenerateUniquePath(tempPath + fileName + ".pdf");

                ConversionPreset intermediatePreset = new ConversionPreset("Pdf to image", this.ConversionPreset, "pdf");
                this.pdf2ImageConversionJob = ConversionJobFactory.Create(intermediatePreset, this.intermediateFilePath);
                this.pdf2ImageConversionJob.PrepareConversion(this.OutputFilePaths);
            }
        }

        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new System.Exception("The conversion preset must be valid.");
            }

            string conversionError = string.Empty;

            if (Helpers.IsMicrosoftOfficeApplicationAvailable(this.Application))
            {
                try
                {
                    this.ConvertWithMicrosoftWord();
                    return;
                }
                catch (Exception exception)
                {
                    conversionError = exception.Message;
                    Debug.Log(exception.ToString());
                    Debug.Log("Microsoft Word conversion failed. Trying fallback converter if available.");
                    this.CloseDocumentIfNeeded();
                    this.ReleaseOfficeApplicationInstanceIfNeeded();
                    this.DeleteIntermediateFileIfNeeded();
                }
            }
            else
            {
                Debug.Log("Microsoft Word is not available. Trying fallback converter if available.");
            }

            if (this.ConversionPreset.OutputType == OutputType.Pdf || this.pdf2ImageConversionJob != null)
            {
                if (this.TryConvertWithLibreOfficeToPdf(this.intermediateFilePath, out string libreOfficeError))
                {
                    if (this.pdf2ImageConversionJob != null)
                    {
                        this.ConvertIntermediatePdfToImagesIfNeeded();
                    }

                    return;
                }

                conversionError = string.IsNullOrEmpty(conversionError) ? libreOfficeError : $"{conversionError}\nLibreOffice fallback failed: {libreOfficeError}";
            }

            if (string.IsNullOrEmpty(conversionError))
            {
                conversionError = Properties.Resources.ErrorUnableToUseMicrosoftOffice;
            }

            this.ConversionFailed(conversionError);
        }


        private void ConvertWithMicrosoftWord()
        {
            this.UserState = Properties.Resources.ConversionStateReadDocument;

            if (!this.TryLoadDocumentIfNecessary())
            {
                throw new InvalidOperationException(Properties.Resources.ErrorUnableToUseMicrosoftOffice);
            }

            // Make this document the active document.
            this.document.Activate();

            this.UserState = Properties.Resources.ConversionStateConversion;

            Debug.Log("Convert word document to pdf.");
            this.document.ExportAsFixedFormat(
                this.intermediateFilePath,
                Word.Enums.WdExportFormat.wdExportFormatPDF,
                false,
                Word.Enums.WdExportOptimizeFor.wdExportOptimizeForPrint,
                Word.Enums.WdExportRange.wdExportAllDocument,
                1,
                1,
                Word.Enums.WdExportItem.wdExportDocumentContent,
                true,
                true,
                Word.Enums.WdExportCreateBookmarks.wdExportCreateNoBookmarks,
                true,
                true,
                false);

            this.EnsureIntermediatePdfExists();
            this.CloseDocumentIfNeeded();
            this.ReleaseOfficeApplicationInstanceIfNeeded();
            this.ConvertIntermediatePdfToImagesIfNeeded();
        }

        private void ConvertIntermediatePdfToImagesIfNeeded()
        {
            if (this.pdf2ImageConversionJob == null)
            {
                return;
            }

            if (!System.IO.File.Exists(this.intermediateFilePath))
            {
                this.ConversionFailed(Properties.Resources.ErrorCantFindOutputFiles);
                return;
            }

            Task updateProgress = this.UpdateProgress();

            Debug.Log("Convert pdf to images.");

            this.pdf2ImageConversionJob.StartConversion();

            if (this.pdf2ImageConversionJob.State != ConversionState.Done)
            {
                this.ConversionFailed(this.pdf2ImageConversionJob.ErrorMessage);
                return;
            }

            if (!string.IsNullOrEmpty(this.intermediateFilePath))
            {
                Debug.Log($"Delete intermediate file {this.intermediateFilePath}.");

                File.Delete(this.intermediateFilePath);
            }

            updateProgress.Wait();
        }


        private void CloseDocumentIfNeeded()
        {
            if (this.document == null)
            {
                return;
            }

            try
            {
                Debug.Log($"Close word document '{this.InputFilePath}'.");
                this.document.Close(Word.Enums.WdSaveOptions.wdDoNotSaveChanges);
            }
            catch (Exception exception)
            {
                Debug.Log($"Failed to close word document '{this.InputFilePath}': {exception}");
            }
            finally
            {
                this.document = null;
            }
        }

        private void EnsureIntermediatePdfExists()
        {
            this.EnsureFileExistsAndIsNotEmpty(this.intermediateFilePath);
        }

        private void DeleteIntermediateFileIfNeeded()
        {
            this.DeleteFileIfNeeded(this.intermediateFilePath, "partial intermediate");
        }

        protected override void InitializeOfficeApplicationInstanceIfNecessary()
        {
            if (this.application != null)
            {
                return;
            }

            // Initialize word application.
            Debug.Log("Instantiate word application via interop.");
            this.application = new Word.Application
            {
                Visible = false,
                DisplayAlerts = Word.Enums.WdAlertLevel.wdAlertsNone,
            };
        }

        protected override void ReleaseOfficeApplicationInstanceIfNeeded()
        {
            if (this.application == null)
            {
                return;
            }

            Diagnostics.Debug.Log("Quit word application via interop.");
            try
            {
                this.application.Quit();
            }
            catch (Exception exception)
            {
                Debug.Log($"Failed to quit word application: {exception}");
            }
            finally
            {
                this.application = null;
            }
        }

        private async Task UpdateProgress()
        {
            while (this.pdf2ImageConversionJob.State != ConversionState.Done &&
                   this.pdf2ImageConversionJob.State != ConversionState.Failed)
            {
                if (this.pdf2ImageConversionJob != null && this.pdf2ImageConversionJob.State == ConversionState.InProgress)
                {
                    this.Progress = this.pdf2ImageConversionJob.Progress;
                }

                if (this.pdf2ImageConversionJob != null && this.pdf2ImageConversionJob.State == ConversionState.InProgress)
                {
                    this.Progress = this.pdf2ImageConversionJob.Progress;
                    this.UserState = this.pdf2ImageConversionJob.UserState;
                }

                await Task.Delay(40);
            }
        }

        private bool TryLoadDocumentIfNecessary()
        {
            try
            {
                this.InitializeOfficeApplicationInstanceIfNecessary();
            }
            catch (Exception exception)
            {
                Debug.Log(exception.ToString());
                Debug.Log("Failed to initialize office application.");
            }

            if (this.application == null)
            {
                return false;
            }

            if (this.document == null)
            {
                Debug.Log($"Load word document '{this.InputFilePath}'.");

                this.document = this.application.Documents.Open(this.InputFilePath, System.Reflection.Missing.Value, true);
            }

            return this.document != null;
        }
    }
}
