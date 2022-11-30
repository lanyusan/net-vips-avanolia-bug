using Avalonia.Controls;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing.Drawing2D;
using NetVips;
using System;

namespace NetVipsAvalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            GenBtn.Click += async (s, e) =>
            {
                StateLabel.Content = "Started...";
                var imageFiles = Directory.GetFiles("images");


                var frames = imageFiles.Select(filename => {
                    var image = NetVips.Image.NewFromFile(filename);
                    var bytes = image.PngsaveBuffer();
                    var imageStream = new MemoryStream(bytes);
                    var bitMap =  new System.Drawing.Bitmap(imageStream);
                    return image;
                }).ToArray();
                var delays = frames.Select( _ => 10).ToArray();
                var framesJoined = NetVips.Image.Arrayjoin(frames, 1);
                var height = frames[0].Height;
                var framesWithMeta = framesJoined.Mutate(mutable =>
                {
                    // Set the number of loops, libvips uses iterations like this:
                    // 0 - set 0 loops (infinite)
                    // 1 - loop once
                    // 2 - loop twice etc.
                    mutable.Set(GValue.GIntType, "loop", 0);

                    // Set the frame delay(s)
                    // We use a 1000ms delay for all frames in this example
                    mutable.Set(GValue.ArrayIntType, "delay", delays);

                    // Set page height
                    mutable.Set(GValue.GIntType, "page-height", height);
                });
                // frames.Pngsave(fileName + "-joined.png");
                DateTimeOffset now = (DateTimeOffset)DateTime.UtcNow;
                var filename = now.ToUnixTimeMilliseconds() + ".gif";
                var filepath = Path.Join("images", filename);
                framesWithMeta.Gifsave(filepath, dither: 0.5, effort: 1, bitdepth: 8);
                StateLabel.Content = filepath;
            };
        }

    }
}