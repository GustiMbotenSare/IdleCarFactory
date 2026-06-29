# Car Factory Idle - GDD Santai (Bahasa Indonesia)

Halo bro. Ini bukan dokumen resmi, jadi santai aja bacanya. Anggap ini aku lagi ngejelasin game-nya ke kamu sambil ngopi. Tujuannya satu: biar kamu paham game-nya mau jadi kayak gimana, dan kamu bisa langsung gas bikin bagian Unity-nya tanpa pusing mikirin logikanya.

## Pembagian kerja kita

Biar jelas dari awal, ini deal-nya:

- **Aku (ako)** pegang otak game-nya: logika, struktur, dan semua mekanik inti. Semua aturan main, rumus ekonomi, cara mobil dibuat dan dijual, itu udah aku tulis di kode C# dan bakal aku jaga biar stabil. Kalau ada aturan main yang berubah, itu bagianku.
- **Kamu** pegang Unity-nya: tampilan, layar, tombol, animasi, suara, dan ngehubungin semua itu ke logika yang udah ada. Intinya kamu nggak perlu mikirin "angka ini dari mana" atau "kenapa harganya segini". Tinggal panggil fungsi yang udah aku siapin, tampilin hasilnya, dengerin event-nya.

Jadi kalau bingung soal mekanik, tanya aku. Kalau soal cara nampilin di layar, itu wilayah kamu.

## Game-nya tentang apa

Car Factory Idle itu game idle / factory. Pemain punya pabrik mobil. Alurnya kayak gini:

1. Gali bahan mentah (besi, karet, tembaga, dan lain-lain).
2. Olah bahan mentah jadi komponen (mesin, sasis, roda, kelistrikan).
3. Rakit komponen jadi mobil utuh. Pas dirakit, mobilnya dapet "grade" acak ala gacha (dari D paling jelek sampai S+ paling bagus).
4. Jual mobilnya lewat showroom, lelang, atau kontrak.
5. Duitnya dipakai upgrade pabrik biar makin cepat dan makin gede, terus muter lagi dari awal.

Target platform-nya WebGL (buat CrazyGames). Mobilnya 12 jenis, namanya sengaja dibikin mirip mobil terkenal tapi nggak nyamain merek asli biar aman.

## Struktur kode (biar kamu nggak nyasar)

Semua logika ada di `Assets/_Project/Scripts/`, kebagi jadi beberapa folder:

- `Core/` = booting game, save/load, sama Facade (ini penting, baca bagian bawah).
- `Data/` = semua definisi dan angka: item, stasiun, mobil, rumus ekonomi, tabel gacha.
- `Events/` = sistem event, buat ngabarin UI kalau ada sesuatu yang terjadi.
- `Simulation/` = mesin gameplay yang jalan tiap frame: produksi, perakitan, lelang, showroom, balapan.
- `State/` = data yang disimpan: isi gudang, dompet, stasiun, kontrak.
- `Platform/` = tempelan buat iklan CrazyGames sama save WebGL (ini perlu kamu isi, lihat di bawah).

## Aturan main paling penting buat kamu: lewat GameFacade

Ini bagian yang wajib kamu inget. Ada satu file namanya `GameFacade.cs`. Anggap ini satu-satunya pintu masuk buat UI.

- Kalau UI mau ngapa-ngapain (beli, upgrade, jual, mulai balapan, dan lain-lain), panggil fungsi di `GameFacade`. Jangan pernah ngubah data game langsung dari UI.
- Buat nampilin angka (duit, jumlah barang, dan lain-lain), UI tinggal baca state-nya.
- Buat tau kapan harus update tampilan, UI dengerin event dari folder `Events/` (lewat `GameEventBus`). Contoh: ada event pas duit berubah, pas mobil kejual, pas balapan selesai, dan ada juga buat toast notifikasi.

Kenapa dibikin gini? Biar logika sama tampilan kepisah rapi. Aku bisa otak-atik logika tanpa ngerusak UI kamu, dan kamu bisa rombak UI tanpa takut ngerusak mekanik. Win-win.

## Sistem-sistem yang ada (dan apa kerjanya)

- **Produksi** (`ProductionSystem.cs`): jalanin stasiun penggali bahan dan stasiun pengolah komponen.
- **Perakitan** (`AssemblySystem.cs`): rakit mobil dari komponen, terus ngerol grade gacha.
- **Lelang** (`AuctionSystem.cs`): lelang 30 detik, ada bot yang naikin tawaran tiap beberapa detik.
- **Showroom** (`ShowroomSystem.cs`): slot pajangan mobil, ada pengunjung yang dateng dan nawar.
- **Kontrak** (`ContractSystem.cs`): bikin dan nyelesain pesanan (standar, borongan, kilat, premium, VIP).
- **Balapan** (`CircuitRaceSystem.cs`): balapan sirkuit. Ini bukan balapan fisika beneran, cuma hitung-hitungan dari stat mobil. Ada juga `RaceSystem.cs` versi lama yang masih disimpen buat referensi.
- **Offline** (`OfflineService.cs`): pas pemain balik lagi setelah lama nggak main, ini ngitung hasil produksi selama dia pergi (maksimal 8 jam).
- **Ekonomi** (`Economy.cs`): semua rumus biaya dan scaling. Kalau kamu butuh tau harga upgrade, jawabannya ada di sini.
- **Gacha** (`GachaRoller.cs`): nentuin peluang grade mobil pas dirakit.

## Soal balapan: mobil apa aja yang bisa ikut

Ini biar nggak salah paham. Buat balapan, pemain bisa pakai DUA macam mobil:

- **Mobil produksi biasa** (mobil yang sama kayak yang dijual). Mobil ini balap pakai stat dasarnya, dikali bonus dari grade gacha-nya. Jadi mobil model sama tapi grade-nya lebih tinggi bakal lebih kenceng. Mobilnya nggak ilang pas dipakai balapan.
- **Mobil balap khusus** (Vulcan GT, Thunderbolt R, Hypernova X). Ini dibikin di jalur produksi khusus, stat-nya pasti (nggak ada gacha), dan disimpen terpisah dari stok showroom.

Di sisi logika, dua-duanya udah didukung. Jadi di layar balapan nanti, kamu bisa nampilin pilihan dari mobil balap khusus DAN mobil produksi yang lagi dipunya pemain. Tinggal panggil `StartRace` di Facade dengan id mobil yang dipilih.

## Yang perlu kamu kerjain di Unity

Ini semacam checklist kasar biar kamu ada gambaran. Nggak harus urut.

1. **Bikin scene utama** dan taruh objek `GameRoot` di situ. `GameRoot` ini yang nyalain semua sistem dan ngejalanin tick tiap frame. Dia juga autosave tiap 30 detik.
2. **Bikin semua layar UI**: pabrik, showroom, lelang, kontrak, balapan, dan layar gacha/hasil rakitan. Hubungin semuanya ke `GameFacade`.
3. **Pasang sprite dan ikon**. Di kode, ikon item sama stasiun sengaja aku kosongin. Jadi gambar mobil, ikon bahan, semua itu kamu yang assign di Unity.
4. **Isi folder `Platform/`**. Ada dua file tempelan yang sekarang masih kosong (cuma biar bisa jalan di editor):
   - `CrazyAds.cs` buat iklan CrazyGames. Sekarang isinya kosong. Nanti pas build WebGL, masukin SDK CrazyGames (v3) di sini.
   - `WebGlSync.cs` buat maksa simpan data di WebGL biar progress nggak ilang pas tab ditutup. Ini juga masih kosong, perlu kamu isi pakai interop JavaScript.
5. **Audio dan feedback**: SFX, toast notifikasi, tutorial maskot. Logika udah ngirim event-nya, kamu tinggal nampilin dan bunyiin.
6. **Cek pakai SimSmokeTest**. Ada file `SimSmokeTest.cs` yang ngejalanin seluruh alur game tanpa UI. Berguna buat mastiin logikanya jalan sebelum kamu repot-repot bikin tampilan.

## Hal-hal kecil yang enak diketahui

- Konten game (item, stasiun, mobil, angka) dibangun langsung di kode lewat `DefaultContent.cs`. Jadi kamu nggak harus bikin file .asset satu-satu biar game-nya jalan.
- Save pakai PlayerPrefs, ada cek versi. Kalau format save lama nggak cocok, otomatis di-reset biar game nggak error.
- Mobil yang lagi dipajang di showroom langsung kepotong dari gudang, dan baru kebayar pas kejual. Jadi nggak mungkin satu mobil kejual dua kali.
- Kalau mau versi lengkap dan rapi (bahasa Inggris, lebih formal), ada di `docs/GDD.md`. Yang ini versi santai, itu versi detailnya.

## Kalau ada yang bingung

Kalau ada mekanik yang nggak jelas atau angkanya kerasa aneh, jangan diutak-atik sendiri di kode logika. Kabarin aku aja, nanti aku yang benerin di sisi logika. Kamu fokus aja bikin game-nya kelihatan dan kerasa enak dimainin. Makasih ya udah bantu, semoga betah.
