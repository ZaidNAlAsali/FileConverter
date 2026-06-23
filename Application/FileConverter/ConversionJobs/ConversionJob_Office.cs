// <copyright file="ConversionJob_Office.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using FileConverter.Diagnostics;

    public abstract class ConversionJob_Office : ConversionJob
    {
        protected ConversionJob_Office() : base()
        {
        }

        protected ConversionJob_Office(ConversionPreset conversionPreset, string inputFilePath) : base(conversionPreset, inputFilePath)
        {
        }

        public enum ApplicationName
        {
            None,

            Word,
            Excel,
            PowerPoint
        }

        protected abstract ApplicationName Application
        {
            get;
        }

        protected override bool IsCancelable() => false;

        protected override void Initialize()
        {
            base.Initialize();

            if (!Helpers.IsMicrosoftOfficeApplicationAvailable(this.Application) && !this.HasAlternativeOfficeConverter())
            {
                switch (this.Application)
                {
                    case ApplicationName.Word:
                        this.ConversionFailed(Properties.Resources.ErrorMicrosoftWordIsNotAvailable);
                        return;

                    case ApplicationName.PowerPoint:
                        this.ConversionFailed(Properties.Resources.ErrorMicrosoftPowerPointIsNotAvailable);
                        return;

                    case ApplicationName.Excel:
                        this.ConversionFailed(Properties.Resources.ErrorMicrosoftExcelIsNotAvailable);
                        return;

                    default:
                        this.ConversionFailed(Properties.Resources.ErrorMicrosoftOfficeIsNotAvailable);
                        return;
                }
            }
        }

        protected virtual bool HasAlternativeOfficeConverter()
        {
            return this.ConversionPreset != null &&
                   this.ConversionPreset.OutputType == OutputType.Pdf &&
                   Helpers.IsLibreOfficeAvailable();
        }

        protected bool OutputRequiresPdfConversion()
        {
            if (this.ConversionPreset == null)
            {
                return false;
            }

            switch (this.ConversionPreset.OutputType)
            {
                case OutputType.Pdf:
                case OutputType.Avif:
                case OutputType.Jpg:
                case OutputType.Png:
                case OutputType.Webp:
                    return true;

                default:
                    return false;
            }
        }

        protected bool TryConvertWithLibreOfficeToPdf(string outputFilePath, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!this.OutputRequiresPdfConversion())
            {
                errorMessage = $"LibreOffice fallback is only available for PDF or image-page outputs. Requested output: {this.ConversionPreset?.OutputType}.";
                Debug.Log(errorMessage);
                return false;
            }

            if (!Helpers.TryGetLibreOfficeExecutablePath(out string libreOfficePath))
            {
                errorMessage = "LibreOffice is not installed or soffice.exe could not be found. Install LibreOffice or set LIBREOFFICE_PATH to soffice.exe.";
                Debug.Log(errorMessage);
                return false;
            }

            string tempDirectory = Path.Combine(Path.GetTempPath(), "ZFileConverter-LibreOffice-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                this.UserState = Properties.Resources.ConversionStateConversion;

                string conversionFilter = this.GetLibreOfficePdfExportFilter();
                string convertToArgument = string.IsNullOrEmpty(conversionFilter) ? "pdf" : "pdf:" + conversionFilter;

                Debug.Log($"Convert Office document to pdf with LibreOffice: {libreOfficePath}.");

                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = libreOfficePath,
                    Arguments = $"--headless --nologo --nofirststartwizard --norestore --nodefault --nolockcheck --convert-to {QuoteArgument(convertToArgument)} --outdir {QuoteArgument(tempDirectory)} {QuoteArgument(this.InputFilePath)}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using (System.Diagnostics.Process process = new System.Diagnostics.Process { StartInfo = startInfo })
                {
                    process.Start();
                    Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

                    if (!process.WaitForExit(120000))
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch (Exception exception)
                        {
                            Debug.Log($"Unable to kill LibreOffice converter after timeout: {exception}");
                        }

                        errorMessage = "LibreOffice conversion timed out after 120 seconds.";
                        Debug.Log(errorMessage);
                        return false;
                    }

                    string standardOutput = standardOutputTask.Result;
                    string standardError = standardErrorTask.Result;

                    if (!string.IsNullOrWhiteSpace(standardOutput))
                    {
                        Debug.Log($"LibreOffice stdout: {standardOutput}");
                    }

                    if (!string.IsNullOrWhiteSpace(standardError))
                    {
                        Debug.Log($"LibreOffice stderr: {standardError}");
                    }

                    if (process.ExitCode != 0)
                    {
                        errorMessage = $"LibreOffice conversion failed with exit code {process.ExitCode}. {standardError}";
                        Debug.Log(errorMessage);
                        return false;
                    }
                }

                string expectedOutputPath = Path.Combine(tempDirectory, Path.GetFileNameWithoutExtension(this.InputFilePath) + ".pdf");
                if (!File.Exists(expectedOutputPath))
                {
                    string[] pdfFiles = Directory.GetFiles(tempDirectory, "*.pdf");
                    if (pdfFiles.Length == 1)
                    {
                        expectedOutputPath = pdfFiles[0];
                    }
                }

                if (!File.Exists(expectedOutputPath))
                {
                    errorMessage = "LibreOffice did not produce a PDF output file.";
                    Debug.Log(errorMessage);
                    return false;
                }

                if (File.Exists(outputFilePath))
                {
                    errorMessage = $"Output file already exists: {outputFilePath}";
                    Debug.Log(errorMessage);
                    return false;
                }

                File.Copy(expectedOutputPath, outputFilePath);
                this.EnsureFileExistsAndIsNotEmpty(outputFilePath);
                Debug.Log($"LibreOffice conversion succeeded: {outputFilePath}.");
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                Debug.Log(exception.ToString());
                return false;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDirectory))
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                }
                catch (Exception exception)
                {
                    Debug.Log($"Unable to delete LibreOffice temp directory '{tempDirectory}': {exception}");
                }
            }
        }

        protected void EnsureFileExistsAndIsNotEmpty(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException(Properties.Resources.ErrorCantFindOutputFiles);
            }

            FileInfo outputFileInfo = new FileInfo(filePath);
            if (outputFileInfo.Length == 0)
            {
                throw new InvalidOperationException(Properties.Resources.ErrorCantFindOutputFiles);
            }
        }

        protected void DeleteFileIfNeeded(string filePath, string description)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            try
            {
                Debug.Log($"Delete {description} file {filePath}.");
                File.Delete(filePath);
            }
            catch (Exception exception)
            {
                Debug.Log($"Unable to delete {description} file '{filePath}': {exception}");
            }
        }

        protected string CombineOfficeAndFallbackErrors(string microsoftOfficeError, string libreOfficeError)
        {
            if (string.IsNullOrEmpty(microsoftOfficeError))
            {
                return libreOfficeError;
            }

            if (string.IsNullOrEmpty(libreOfficeError))
            {
                return microsoftOfficeError;
            }

            return $"Microsoft Office conversion failed: {microsoftOfficeError}\nLibreOffice fallback failed: {libreOfficeError}";
        }

        protected override void OnConversionFailed()
        {
            base.OnConversionFailed();

            this.ReleaseOfficeApplicationInstanceIfNeeded();
        }

        protected abstract void InitializeOfficeApplicationInstanceIfNecessary();

        protected abstract void ReleaseOfficeApplicationInstanceIfNeeded();

        private string GetLibreOfficePdfExportFilter()
        {
            switch (this.Application)
            {
                case ApplicationName.Word:
                    return "writer_pdf_Export";

                case ApplicationName.Excel:
                    return "calc_pdf_Export";

                case ApplicationName.PowerPoint:
                    return "impress_pdf_Export";

                default:
                    return null;
            }
        }

        private static string QuoteArgument(string argument)
        {
            if (argument == null)
            {
                return "\"\"";
            }

            return "\"" + argument.Replace("\"", "\\\"") + "\"";
        }
    }
}
