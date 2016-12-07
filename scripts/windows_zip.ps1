Add-Type -Assembly “system.io.compression.filesystem”
Add-Type -Assembly “system.io.compression"

$ErrorActionPreference = "Stop"

if($args.Count -eq 0)
{
    exit(1)
}

$source = $args[0];

$zip = [io.compression.zipfile]::Open($source + "\binaries.zip", [System.IO.Compression.ZipArchiveMode]::Update)

$files = [System.IO.Directory]::GetFiles($source)

for ($i=0; $i -lt $files.length; $i++) 
{
    $filename = [System.IO.Path]::GetFileName($files[$i]);
    
    if(!($filename -ceq "binaries.zip") -and !($filename -ceq ".gitignore"))
    {
	    Write-Host ("compressing " + $filename)
        
        $entry = $zip.CreateEntry($filename);
        $stream = $entry.Open();
        $fstream = [System.IO.File]::OpenRead($source + "\" + $filename);
        $fstream.CopyTo($stream);

        $fstream.Close();
        $stream.Close();
    }
}

$zip.Dispose()

exit(0)