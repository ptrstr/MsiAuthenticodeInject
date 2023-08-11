# MsiAuthenticodeInject

This project demonstrates a proof of concept bypass to Microsoft's optional patch of CVE-2013-3900.

## How it works?
Microsoft's MSI files have Authenticode signatures stored in their `\x05DigitalSignature` entry. Using the same strategy used in CVE-2013-3900, we can append data to the end of the stream, updating the relevant fields from the MSI (size for instance), while preserving the signature of the MSI file.

Microsoft's optional patch of the CVE does not seem to cover MSI files. 

## Why it works?

### Finding the patch
CVE-2013-3900, patched most recently in [KB2893294 (MS13-098)](https://www.catalog.update.microsoft.com/Search.aspx?q=MS13-098), has a very selective patch. Looking at the update details, we can only see 2 DLL files having received an update.

```xml
<!-- amd64_microsoft-windows-coreos_31bf3856ad364e35_6.3.9600.16438_none_164ab8fc121c2479.manifest -->
...
<file name="imagehlp.dll" destinationPath="$(runtime.system32)\" sourceName="imagehlp.dll" sourcePath=".\" importPath="$(build.nttree)\">
	...
</file>
<file name="wmi.dll" destinationPath="$(runtime.system32)\" sourceName="wmi.dll" sourcePath=".\" importPath="$(build.nttree)\">
	...
</file>
...
```

The patch consists of adding the `EnableCertPaddingCheck` key to the registry. Doing a search for this string in both files yields a match for `imagehlp.dll`

### Investigating the patch
Searching for this symbol in the binary yields one usage from `NeedCheckEndGap`:
```c
BOOL NeedCheckEndGap(void)
{
  LSTATUS status;
  DWORD data;
  DWORD size;
  HKEY wintrust_config_key;
  
  wintrust_config_key = (HKEY)0x0;
  data = 0;
  size = 4;
  if (_EnableCertPaddingCheck_is_set == 0) {
    _EnableCertPaddingCheck_is_set = 1;
    status = RegOpenKeyExW((HKEY)0xffffffff80000002,
                           L"Software\\Microsoft\\Cryptography\\Wintrust\\Config",0,0x20019,
                           &wintrust_config_key);
    if (status == ERROR_SUCCESS) {
      status = RegQueryValueExW(wintrust_config_key,L"EnableCertPaddingCheck",(LPDWORD)0x0,
                                (LPDWORD)0x0,(LPBYTE)&data,&size);
      if (status == 0) {
        EnableCertPaddingCheck = (BOOL)(data != 0);
      }
    }
    if (wintrust_config_key != (HKEY)0x0) {
      RegCloseKey(wintrust_config_key);
    }
  }
  return EnableCertPaddingCheck;
}
```

This symbol is only referenced from `imagehlp.dll`, where only PE files are handled. Therefore, any usage of the same vulnerability in the MSI format (or possibly others theoretically) yields signed binaries despite being injected.
