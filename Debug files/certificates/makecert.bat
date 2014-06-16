makecert.exe -sv RootCATest.pvk -r -n "CN=localhost" RootCATest.cer
makecert.exe -ic RootCATest.cer -iv RootCATest.pvk -n "CN=localhost" -sv  TempCert.pvk -pe -sky exchange TempCert.cer
Cert2Spc.exe TempCert.cer TempCert.spc
pvkimprt.exe -pfx TempCert.spc TempCert.pvk