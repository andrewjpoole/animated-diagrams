using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AnimatedDiagrams.Services;
using Xunit;
using static AnimatedDiagrams.Tests.ThemeTests;

namespace AnimatedDiagrams.Tests
{
    public class SvgImportExportTests
    {
        [Fact]
        public void ImportExport_RoundTrip_MatchesPathCountAndFirstPath()
        {
            // Arrange
            var projectDir = AppContext.BaseDirectory;
            while (!Directory.Exists(Path.Combine(projectDir, "testData")))
                projectDir = Path.GetDirectoryName(projectDir)!;
            var svgPath = Path.Combine(projectDir, "testData", "concepts.svg");
            var originalXml = File.ReadAllText(svgPath);

            var pensService = new PensService();
            var settingsService = new SettingsService(new DummyStorage());
            var undoRedoservice = new UndoRedoService();
            var editor = new PathEditorState(pensService, settingsService, undoRedoservice);
            var service = new SvgFileService(editor);
            service.ImportSvg(originalXml);

            // Act
            var exportedXml = service.ExportSvg();

            // Parse original and exported SVGs
            var origDoc = XDocument.Parse(originalXml);
            var expDoc = XDocument.Parse(exportedXml);
            var origPaths = origDoc.Descendants().Where(e => e.Name.LocalName == "path").ToList();
            var expPaths = expDoc.Descendants().Where(e => e.Name.LocalName == "path").ToList();

            // Assert
            Assert.True(origPaths.Count > 0, "Original SVG should have at least one path");
            Assert.Equal(origPaths.Count, expPaths.Count);
            Assert.Equal(origPaths[0].Attribute("d")?.Value, expPaths[0].Attribute("d")?.Value);
        }
    }
}
