//
// ColumnCellCoverArt.cs
//
// Author:
//   Nicholas Little <arealityfarbetween@googlemail.com>
//
// Copyright (C) 2017
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;
using Cairo;

using Hyena.Gui;
using Hyena.Gui.Theming;
using Hyena.Data.Gui;
using Hyena.Data.Gui.Accessibility;

using Banshee.Gui;
using Banshee.ServiceStack;

namespace Banshee.Collection.Gui
{
    public class ColumnCellCoverArt : ColumnCell
    {
        private static int image_spacing = 4;
        private static int image_size = 48;

        private ArtworkManager artwork_manager;

        public ColumnCellCoverArt () : base (null, true)
        {
            artwork_manager = ServiceManager.Get<ArtworkManager> ();

            FixedSize = new Hyena.Gui.Canvas.Size ( image_size, image_size );
        }

        private class ColumnCellCoverArtAccessible : ColumnCellAccessible
        {
            public ColumnCellCoverArtAccessible (object bound_object, ColumnCellCoverArt cell, ICellAccessibleParent parent)
                : base (bound_object, cell as ColumnCell, parent)
            {
                var bound_album_info = bound_object as AlbumInfo;
                if (bound_album_info != null) {
                    Name = String.Format ("{0} - {1}",
                                         bound_album_info.DisplayTitle,
                                         bound_album_info.DisplayArtistName);
                }
            }
        }

        public override Atk.Object GetAccessible (ICellAccessibleParent parent)
        {
            return new ColumnCellCoverArtAccessible (BoundObject, this, parent);
        }

        public override void Render (CellContext context, double cellWidth, double cellHeight)
        {
            if (BoundObject == null) {
                return;
            }

            TrackInfo track = BoundObject as TrackInfo;
            AlbumInfo album = BoundObject as AlbumInfo;

            if (null == track && null == album) {
                throw new InvalidCastException ("ColumnCellCoverArt can only bind to Track/AlbumInfo objects");
            }

            string artworkId = track?.ArtworkId ?? album?.ArtworkId;

            int image_render_size = (int) Math.Min(cellWidth, cellHeight);

            ImageSurface image = artwork_manager?.LookupScaleSurface (artworkId, image_render_size, true);

            var x = (cellWidth  - image_render_size) / 2;
            var y = (cellHeight - image_render_size) / 2;

            ArtworkRenderer.RenderThumbnail (context.Context, image,
                dispose:    false,
                x:          (int) x,
                y:          (int) y,
                width:      image_render_size,
                height:     image_render_size, 
                drawBorder: false,
                radius:     0);
        }
    }
}
