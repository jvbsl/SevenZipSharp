// See https://aka.ms/new-console-template for more information

using SevenZip;

void ExtractArchiveMultiVolumesTest()
{
    const string OutputDirectory = "output";
    using (var extractor = new SevenZipExtractor(@"TestData/multivolume.part0001.rar"))
    {
        extractor.ExtractArchive(OutputDirectory);
    }

    Console.WriteLine(1 == Directory.GetFiles(OutputDirectory).Length);
    Console.WriteLine(File.ReadAllText(Directory.GetFiles(OutputDirectory)[0]).StartsWith("Lorem ipsum dolor sit amet"));
}
void CreateSfxArchiveTest()
{
    var sfxModule = SfxModule.Simple;
    const string OutputDirectory = "output";
    string TemporaryFile = Path.Combine(OutputDirectory, "tmp.7z");
    if (sfxModule.HasFlag(SfxModule.Custom))
    {
        //Assert.Ignore("No idea how to use SfxModule \"Custom\".");
    }

    var sfxFile = Path.Combine(OutputDirectory, "sfx.exe");
    var sfx = new SevenZipSfx(sfxModule);
    var compressor = new SevenZipCompressor {DirectoryStructure = false};

    compressor.CompressDirectory("TestData", TemporaryFile);
    /*sfx.MakeSfx(TemporaryFile, sfxFile);

    Console.WriteLine(File.Exists(sfxFile));

    using (var extractor = new SevenZipExtractor(sfxFile))
    {
        Console.WriteLine(1 == extractor.FilesCount);
        Console.WriteLine("zip.zip" == extractor.ArchiveFileNames[0]);
    }*/
}
ExtractArchiveMultiVolumesTest();