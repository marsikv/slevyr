        ukazky volani API

		http://localhost:5000/api/slevy/nastavOkNg?ok=10&ng=5

        http://localhost:5000/api/slevy/nastavDefektivitu?varianta=A&def1=1&def2=33&def3=44

        http://localhost:5000/api/slevy/vratitStavCitacu

        http://localhost:5000/api/slevy/status

        http://localhost:5000/api/slevy/closePort


		//URL reservation - run as admin
        netsh http add urlacl url=http://+:5000/ user=Everyone

		//delete URL reservation - run as admin
        netsh http delete urlacl url=http://+:5000/ user=Everyone