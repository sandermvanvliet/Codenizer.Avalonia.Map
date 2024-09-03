# Copyright (c) 2023 Sander van Vliet
# Licensed under GNU General Public License v3.0
# See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

$csharpFiles = Get-ChildItem -Recurse -Filter *.cs | where-object {!$_.fullname.Contains("\obj\") -and !$_.fullname.Contains("\bin\")}

$prepend = Get-Content "csharp-license.txt" -Raw

foreach($file in $csharpFiles)
{
    $fullPath = $file.FullName
    $contents = get-content $fullPath -head 1
    
    if(!($contents.Trim().StartsWith("// Copyright (c)")))
    {
        Write-Host "$fullPath doesn't have a license header"

        $rawContents = Get-Content $file -Raw

        $targetContent = $prepend + $rawContents
        Set-Content $file $targetContent
    }
}