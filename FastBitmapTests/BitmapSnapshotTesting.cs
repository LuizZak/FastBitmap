/*
    Pixelaria
    Copyright (C) 2013 Luiz Fernando Silva

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

    The full license may be found on the License.txt file attached to the
    base directory of this project.
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FastBitmapLib;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FastBitmapTests
{
    /// <summary>
    /// Helper static class to perform bitmap-based rendering comparisons as assertions.
    /// </summary>
    public static class BitmapSnapshotTesting
    {
        /// <summary>
        /// If true, when generating output folders for test results, paths are created for each segment of the namespace
        /// of the target test class, e.g. 'PixUI.Controls.LabelControlViewTests' becomes '...\PixUI\Controls\LabelControlViewtests\',
        /// otherwise a single folder with the fully-qualified class name is used instead.
        /// 
        /// If this property is changed across test recordings, the tests must be re-recorded to account for the new directory paths
        /// expected by the snapshot class.
        /// 
        /// Defaults to false.
        /// </summary>
        public static bool SeparateDirectoriesPerNamespace = false;

        /// <summary>
        /// Performs a snapshot text with a given test context/object pair, using an instantiable snapshot provider.
        /// </summary>
        public static void Snapshot<TProvider, TObject>([NotNull] TObject source, [NotNull] TestContext context, bool recordMode) where TProvider : ISnapshotProvider<TObject>, new()
        {
            var provider = new TProvider();

            Snapshot(provider, source, context, recordMode);
        }

        /// <summary>
        /// Performs a snapshot text with a given test context/object pair, using a given instantiated snapshot provider.
        /// </summary>
        public static void Snapshot<T>([NotNull] ISnapshotProvider<T> provider, [NotNull] T target, [NotNull] TestContext context, bool recordMode)
        {
            string targetPath = CombinedTestResultPath(TestResultsPath(), context);

            // Verify path exists
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            string testFileName = context.TestName + ".png";
            string testFilePath = Path.Combine(targetPath, testFileName);

            // Verify comparison file's existence (if not in record mode)
            if (!recordMode)
            {
                if(!File.Exists(testFilePath))
                    Assert.Fail($"Could not find reference image file {testFilePath} to compare. Please re-run the test with {nameof(recordMode)} set to true to record a test result to compare later.");
            }
            
            var image = provider.GenerateBitmap(target);

            if (recordMode)
            {
                image.Save(testFilePath, ImageFormat.Png);

                Assert.Fail(
                    $"Saved image to path {testFilePath}. Re-run test mode with {nameof(recordMode)} set to false to start comparing with record test result.");
            }
            else
            {
                // Load recorded image and compare
                using (var expected = (Bitmap)Image.FromFile(testFilePath))
                using (var expLock = expected.FastLock())
                using (var actLock = image.FastLock())
                {
                    bool areEqual = expLock.Width == actLock.Width && expLock.DataArray.SequenceEqual(actLock.DataArray);
                    
                    if (areEqual)
                        return; // Success!

                    // Save to test results directory for further inspection
                    string directoryName = CombinedTestResultPath(context.TestDir, context);
                    string baseFileName = Path.ChangeExtension(testFileName, null);

                    string savePathExpected = Path.Combine(directoryName, Path.ChangeExtension(baseFileName + "-expected", ".png"));
                    string savePathActual = Path.Combine(directoryName, Path.ChangeExtension(baseFileName + "-actual", ".png"));

                    // Ensure path exists
                    if (!Directory.Exists(directoryName))
                    {
                        Assert.IsNotNull(directoryName, "directoryName != null");
                        Directory.CreateDirectory(directoryName);
                    }

                    image.Save(savePathActual, ImageFormat.Png);
                    expected.Save(savePathExpected, ImageFormat.Png);

                    context.AddResultFile(savePathActual);

                    Assert.Fail($"Resulted image did not match expected image. Inspect results under directory {directoryName} for info about results");
                }
            }
        }
        
        private static string CombinedTestResultPath([NotNull] string basePath, [NotNull] TestContext context)
        {
            if(!SeparateDirectoriesPerNamespace)
                return Path.Combine(basePath, context.FullyQualifiedTestClassName);

            var segments = context.FullyQualifiedTestClassName.Split('.');

            return Path.Combine(new[] {basePath}.Concat(segments).ToArray());
        }

        private static string TestResultsPath()
        {
            string path = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

            if (path.EndsWith(@"bin\Debug") || path.EndsWith(@"bin\Release"))
            {
                return Path.GetFullPath(Path.Combine(path, @"..\..\Snapshot\Files"));
            }

            if (Regex.IsMatch(path, @"bin\\Debug\\net\d+") || Regex.IsMatch(path, @"bin\\Release\\net\d+"))
            {
                return Path.GetFullPath(Path.Combine(path, @"..\..\..\Snapshot\Files"));
            }
            if (path.EndsWith(@"bin\Debug") || path.EndsWith(@"bin\Release"))
            {
                return Path.GetFullPath(Path.Combine(path, @"..\..\Snapshot\Files"));
            }

            Assert.Fail($@"Invalid/unrecognized test assembly path {path}: Path must end in either bin\[Debug|Release] or bin\[Debug|Release]\[netxyz|netcore|netstandard]");

            return path;
        }
    }

    /// <summary>
    /// Base interface for objects instantiated to provide bitmaps for snapshot tests
    /// </summary>
    /// <typeparam name="T">The type of object this snapshot provider receives in order to produce snapshots.</typeparam>
    public interface ISnapshotProvider<in T>
    {
        /// <summary>
        /// Asks this snapshot provider to create a <see cref="T:System.Drawing.Bitmap"/> from a given object context.
        /// </summary>
        [NotNull]
        Bitmap GenerateBitmap([NotNull] T context);
    }
}
