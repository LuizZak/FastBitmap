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

using System.Drawing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FastBitmapTests
{
    /// <inheritdoc />
    /// <summary>
    /// A pre-implemented snapshot provider for Bitmap-comparison tests.
    /// </summary>
    public class BitmapSnapshot : ISnapshotProvider<Bitmap>
    {
        /// <summary>
        /// Whether tests are currently under record mode- under record mode, results are recorded on disk to be later
        /// compared when not in record mode.
        ///
        /// Calls to <see cref="Snapshot"/> always fail with an assertion during record mode.
        ///
        /// Defaults to false.
        /// </summary>
        public static bool RecordMode = false;

        public static void Snapshot([NotNull] Bitmap bitmap, [NotNull] TestContext context)
        {
            BitmapSnapshotTesting.Snapshot<BitmapSnapshot, Bitmap>(bitmap, context, RecordMode);
        }

        public Bitmap GenerateBitmap(Bitmap context)
        {
            return context;
        }
    }
}
