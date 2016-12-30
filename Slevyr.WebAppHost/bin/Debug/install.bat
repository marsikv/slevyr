@echo off
rem
rem Uplna cesta k souboru se sluzbou
SET FILENAME=c:\devel\apam\slevyr\Slevyr.WebAppHost\bin\Debug\Slevyr.WebAppHost.exe
rem
rem Kratke jmeno sluzby, ktere se pouziva pro jeji spusteni/zastaveni
SET SHORTNAME=Slevyr
rem
rem Dlouhe jmeno sluzby, ktere je videt ve spravci sluzeb
SET LONGNAME=Sledovani výroby - apam
rem 
rem 
sc Create "%SHORTNAME%" start= auto binPath= "%FILENAME%" displayName= "%LONGNAME%"
