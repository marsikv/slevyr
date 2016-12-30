Instalace 

1. Rezervace portu (podmínka provozování v režimu webového serveru)
		//URL reservation - run as admin
        netsh http add urlacl url=http://+:5000/ user=Everyone

		//delete URL reservation - run as admin
        netsh http delete urlacl url=http://+:5000/ 

		místo 5000 je možné dosadit libovolný jiný neobsazený port

2. Instalace sluzby

spustit install.bat
(zkontrolovat platnost nastaveni cesty k exe)

3. Ovládání služby 

start/stop:
  sc start slevyr
  sc stop slevyr
  (pause a continue nejsou nyni podporovane)

4. Odstranění služby
  sc delete slevyr
  

-----------

Krome registrace aplikace jako nativni sluby OS je mozne aplikaci spustit z prikazove radky s jakymkoli parametrem (napriklad: -start) a tim se spusti jako konzolova aplikace.
Z Visual Studia se projekt automaticky spousti jako konzolova aplikace.
K ukonceni aplikace v konzolovem rezimu dojde tak, ze se z klavesnice zada slovo "exit" bez uvozovek a potvrdi se klavesou Enter.

-----------

Aplikace pro svuj beh vyzaduje nainstalovany .NET Framework minimalne verze 4.5

-----------

        ukazky pouzit Web API

		http://localhost:5000/api/slevy/nastavOkNg?ok=10&ng=5

        http://localhost:5000/api/slevy/nastavDefektivitu?varianta=A&def1=1&def2=33&def3=44

        http://localhost:5000/api/slevy/vratitStavCitacu

        http://localhost:5000/api/slevy/status


