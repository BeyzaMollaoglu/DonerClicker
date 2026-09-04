using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    // Dil değiştiğinde tetiklenecek event
    public static event Action OnLanguageChanged;

    public enum Language { Turkish, English }
    public Language currentLanguage = Language.Turkish;

    // Çeviri veritabanı: Dictionary<KelimeAnahtarı, Dictionary<Dil, Çeviri>>
    private Dictionary<string, Dictionary<Language, string>> localizedTexts;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDictionary()
    {
        localizedTexts = new Dictionary<string, Dictionary<Language, string>>();

        // REKLAM (ADS) MANAGER METİNLERİ
        AddText("ad_offer_title", "ÜRETİM HIZI", "PRODUCTION BOOST");
        AddText("ad_offer_desc", "{0} saat boyunca - bedava", "for {0} hours - free");
        AddText("ad_btn_get", "×{0} AL", "GET ×{0}");
        AddText("ad_status_active", "<color=#F0B441>×{0} çalışıyor</color>  -  {1} kaldı", "<color=#F0B441>×{0} active</color>  -  {1} left");
        AddText("ad_status_ready", "<color=#9CB84A>Ödülün hazır - kısa bir video, anında hız</color>", "<color=#9CB84A>Reward ready - short video, instant boost</color>");

        // UIMANAGER - ZAMAN BİRİMLERİ
        AddText("time_unit_sa", "sa", "h");
        AddText("time_unit_dk", "dk", "m");
        AddText("time_unit_sn", "sn", "s");
        AddText("time_unit_saat", "saat", "hours");
        AddText("time_unit_dakika", "dakika", "minutes");
        AddText("time_unit_saniye", "saniye", "seconds");

        // UIMANAGER - DİĞER METİNLER
        AddText("rate_per_sec", "/sn", "/sec");
        AddText("offline_worked", "{0} boyunca çalıştı", "Worked for {0}");
        AddText("offline_worked_capped", "{0} boyunca çalıştı\n<color=#A38A6E>(en fazla bu kadar birikir)</color>", "Worked for {0}\n<color=#A38A6E>(max accumulation reached)</color>");
        AddText("offline_slices", "+{0} dilim", "+{0} slices");
        
        // PRESTİJ (Önceki mesajda eklemiştik ama yoksa diye tekrar koyuyorum)
        AddText("prestige_ready", "Prestij yaparsan <color=#F0B441>+{0} Altın Maşa</color> kazanırsın.\n<color=#A38A6E>Dilimler, işçiler ve geliştirmeler sıfırlanır;\naşağıdaki kalıcı yükseltmeler kalır.</color>", "If you prestige, you will gain <color=#F0B441>+{0} Golden Tongs</color>.\n<color=#A38A6E>Slices, workers, and upgrades will reset;\npermanent upgrades below will remain.</color>");
        AddText("prestige_not_ready", "<color=#A38A6E>Prestij için henüz yeterli üretim yok.\nToplam ürettiğin dilim arttıkça Altın Maşa kazanırsın.</color>", "<color=#A38A6E>Not enough production for prestige yet.\nYou will earn Golden Tongs as your total slices increase.</color>");

        // --- WORKER MANAGER METİNLERİ ---
        AddText("worker_level", "Seviye {0}", "Level {0}");
        AddText("worker_level_multi", "Seviye {0} <color=#9CB84A>+{1}</color>", "Level {0} <color=#9CB84A>+{1}</color>");
        AddText("worker_prod_arrow", "{0}/sn   <color=#9CB84A>> {1}/sn</color>", "{0}/sec   <color=#9CB84A>> {1}/sec</color>");
        AddText("worker_cost", "{0} dilim", "{0} slices");
        AddText("worker_fallback", "{0}\nSeviye: {1}\nÜretim: {2}/sn\nFiyat: {3} dilim", "{0}\nLevel: {1}\nProduction: {2}/sec\nCost: {3} slices");

        // UPGRADE MANAGER METİNLERİ
        AddText("upg_purchased", "Satın alındı", "Purchased");
        AddText("upg_bought", "ALINDI", "BOUGHT");
        AddText("upg_new", "YENİ", "NEW");
        AddText("upg_target_click", "Tıklama gücü", "Click power");
        AddText("upg_target_all", "Tüm üretim", "All production");
        AddText("upg_effect_click_percent", "Tıklama: üretimin +%{0}'i", "Click: +{0}% of production");
        AddText("upg_effect_power", "Güç: {0}{1}", "Power: {0}{1}");
        AddText("upg_cost", "{0} dilimler", "{0} slices");

        // --- ONBOARDING ---
        AddText("onb_tap", "Ortadaki <color=#F0B441>dönere dokun</color> ve dilimlemeye başla.", "Tap the <color=#F0B441>doner</color> in the center to start slicing.");
        AddText("onb_worker", "Dilimlerin birikti! Alttaki <color=#F0B441>İŞÇİLER</color> sekmesinden\nilk ustanı işe al - senin yerine kessin.", "Slices accumulated! Hire your first chef from the <color=#F0B441>WORKERS</color> tab below.");
        AddText("onb_produce", "Ustan çalışıyor: artık sen durunca da dilim geliyor.\nKesmeye devam et, biriktikçe yeni usta al.", "Chef is working: you now get slices automatically.\nKeep slicing and hire more chefs.");
        AddText("onb_upgrade", "Alabileceğin bir <color=#F0B441>geliştirme</color> var!\nAlttaki GELİŞTİRMELER sekmesine bak.", "You have an available <color=#F0B441>upgrade</color>!\nCheck the UPGRADES tab below.");
        AddText("onb_prestige", "Artık <color=#F0B441>PRESTİJ</color> atabilirsin! Dilimlerin ve ustaların\nsıfırlanır ama kalıcı <color=#F0B441>Altın Maşa</color> kazanırsın.", "You can now <color=#F0B441>PRESTIGE</color>! Slices and chefs will reset, but you will earn permanent <color=#F0B441>Golden Tongs</color>.");
   
        // --- WORKERS (İŞÇİLER) ÇEVİRİLERİ ---
        AddText("Stajyer Çırak", "Stajyer Çırak", "Intern Apprentice");
        AddText("Otomatik Bıçak", "Otomatik Bıçak", "Auto Knife");
        AddText("Döner Ustası", "Döner Ustası", "Doner Master");
        AddText("Motorlu Kurye", "Motorlu Kurye", "Motorcycle Courier");
        AddText("Lavaş Makinesi", "Lavaş Makinesi", "Lavash Machine");
        AddText("Şube Müdürü", "Şube Müdürü", "Branch Manager");
        AddText("Franchise Ağı", "Franchise Ağı", "Franchise Network");
        AddText("Döner Fabrikası", "Döner Fabrikası", "Doner Factory");
        AddText("Merkez Depo", "Merkez Depo", "Central Warehouse");
        AddText("Yapay Zeka Usta", "Yapay Zeka Usta", "AI Chef");
        AddText("Gezegenler Arası Kargo", "Gezegenler Arası Kargo", "Interplanetary Cargo");
        AddText("Yörünge Lokantası", "Yörünge Lokantası", "Orbital Restaurant");
        AddText("Kuantum Sentezleyici", "Kuantum Sentezleyici", "Quantum Synthesizer");
        AddText("Yıldız Sistemi Zinciri", "Yıldız Sistemi Zinciri", "Star System Chain");
        AddText("Çoklu Evren Şubesi", "Çoklu Evren Şubesi", "Multiverse Branch");
        AddText("Sonsuzluk Ocağı", "Sonsuzluk Ocağı", "Infinity Grill");

        // --- PRESTİJ İSİMLERİ VE AÇIKLAMALARI ---
        AddText("Bereketli Ocak", "Bereketli Ocak", "Fruitful Grill");
        AddText("Tüm üretim x1.10", "Tüm üretim x1.10", "All production x1.10");
        
        AddText("Keskin Bıçak", "Keskin Bıçak", "Sharp Knife");
        AddText("Tıklama gücü x1.15", "Tıklama gücü x1.15", "Click power x1.15");
        
        AddText("Miras Kalan Tezgah", "Miras Kalan Tezgah", "Inherited Counter");
        AddText("Resetten sonra ilk 8 işçi +3 sv.", "Resetten sonra ilk 8 işçi +3 sv.", "First 8 workers +3 lv after reset");
        
        AddText("Uzun Mesai", "Uzun Mesai", "Long Overtime");
        AddText("Çevrimdışı süre +2 saat", "Çevrimdışı süre +2 saat", "Offline time +2 hours");
        
        AddText("Gece Vardiyası", "Gece Vardiyası", "Night Shift");
        AddText("Çevrimdışı kazanç x1.06", "Çevrimdışı kazanç x1.06", "Offline earnings x1.06");
        
        AddText("Altın Koku", "Altın Koku", "Golden Aroma");
        AddText("Ekrandaki Altın Döner %6 sık gelir", "Ekrandaki Altın Döner %6 sık gelir", "Golden Doner spawns 6% more often");
        
        AddText("Cömert Usta", "Cömert Usta", "Generous Master");
        AddText("Altın Döner ödülü x1.20 güçlenir", "Altın Döner ödülü x1.20 güçlenir", "Golden Doner reward 1.20x stronger");
        
        AddText("Reklam Anlaşması", "Reklam Anlaşması", "Ad Deal");
        AddText("Reklam boostu +0.5x", "Reklam boostu +0.5x", "Ad boost +0.5x");
        
        AddText("Parmak Kası", "Parmak Kası", "Finger Muscle");
        AddText("Tıklama: üretimin +%1'i", "Tıklama: üretimin +%1'i", "Click: +1% of production");
        
        AddText("Toptancı Dostu", "Toptancı Dostu", "Wholesaler Friend");
        AddText("İşçi maliyeti -%4", "İşçi maliyeti -%4", "Worker cost -4%");
        
        AddText("Baharat Sırrı", "Baharat Sırrı", "Spice Secret");
        AddText("Tüm üretim x1.5", "Tüm üretim x1.5", "All production x1.5");
        
        AddText("Efsanevi Şiş", "Efsanevi Şiş", "Legendary Skewer");
        AddText("Tüm üretim x3", "Tüm üretim x3", "All production x3");

// --- GELİŞTİRMELER (UPGRADES) TAM LİSTE ---
        AddText("Bilenmiş Bıçak (Tıklama x2)", "Bilenmiş Bıçak (Tıklama x2)", "Sharpened Knife (Click x2)");
        AddText("Servis Tepsisi (Stajyer Çırak x2)", "Servis Tepsisi (Stajyer Çırak x2)", "Serving Tray (Intern Apprentice x2)");
        AddText("Plastik Eldiven (Tıklama x2)", "Plastik Eldiven (Tıklama x2)", "Plastic Gloves (Click x2)");
        AddText("Vardiya Çizelgesi (Stajyer Çırak x2)", "Vardiya Çizelgesi (Stajyer Çırak x2)", "Shift Schedule (Intern Apprentice x2)");
        AddText("Ergonomik Sap (Tıklama x3)", "Ergonomik Sap (Tıklama x3)", "Ergonomic Handle (Click x3)");
        AddText("Bileme Taşı (Otomatik Bıçak x2)", "Bileme Taşı (Otomatik Bıçak x2)", "Whetstone (Auto Knife x2)");
        AddText("Gizli Sos Formülü (Global x1.5)", "Gizli Sos Formülü (Global x1.5)", "Secret Sauce Formula (Global x1.5)");
        AddText("Çırak Akademisi (Stajyer Çırak x2)", "Çırak Akademisi (Stajyer Çırak x2)", "Apprentice Academy (Intern Apprentice x2)");
        AddText("Titanyum Ağız (Otomatik Bıçak x2)", "Titanyum Ağız (Otomatik Bıçak x2)", "Titanium Edge (Auto Knife x2)");
        AddText("Çift Bıçak (Tıklama = üretimin %0.4'i)", "Çift Bıçak (Tıklama = üretimin %0.4'i)", "Double Knife (Click = 0.4% of production)");
        AddText("Ustalık Belgesi (Döner Ustası x2)", "Ustalık Belgesi (Döner Ustası x2)", "Certificate of Mastery (Doner Master x2)");
        AddText("Bol Malzeme (Global x1.5)", "Bol Malzeme (Global x1.5)", "Abundant Ingredients (Global x1.5)");
        AddText("Otomatik Yağlama (Otomatik Bıçak x2)", "Otomatik Yağlama (Otomatik Bıçak x2)", "Auto Lubrication (Auto Knife x2)");
        AddText("Şef Önlüğü (Döner Ustası x2)", "Şef Önlüğü (Döner Ustası x2)", "Chef Apron (Doner Master x2)");
        AddText("Usta Eli (Tıklama = üretimin %1'i)", "Usta Eli (Tıklama = üretimin %1'i)", "Master's Hand (Click = 1% of production)");
        AddText("Maaş Zammı (Stajyer Çırak x2)", "Maaş Zammı (Stajyer Çırak x2)", "Salary Raise (Intern Apprentice x2)");
        AddText("Termal Çanta (Motorlu Kurye x2)", "Termal Çanta (Motorlu Kurye x2)", "Thermal Bag (Motorcycle Courier x2)");
        AddText("Ulusal Reklam (Global x1.8)", "Ulusal Reklam (Global x1.8)", "National Advertisement (Global x1.8)");
        AddText("Gizli Baharat (Döner Ustası x2)", "Gizli Baharat (Döner Ustası x2)", "Secret Spice (Doner Master x2)");
        AddText("Turbo Motor (Motorlu Kurye x2)", "Turbo Motor (Motorlu Kurye x2)", "Turbo Engine (Motorcycle Courier x2)");
        AddText("Kuantum Tıklama (Tıklama = üretimin %1.8'i)", "Kuantum Tıklama (Tıklama = üretimin %1.8'i)", "Quantum Click (Click = 1.8% of production)");
        AddText("Mahalle Esnafı (Global x1.25)", "Mahalle Esnafı (Global x1.25)", "Neighborhood Tradesman (Global x1.25)");
        AddText("Çift Motor (Otomatik Bıçak x2)", "Çift Motor (Otomatik Bıçak x2)", "Dual Engine (Auto Knife x2)");
        AddText("Taş Fırın (Lavaş Makinesi x2)", "Taş Fırın (Lavaş Makinesi x2)", "Stone Oven (Lavash Machine x2)");
        AddText("Zincir Anlaşması (Global x1.8)", "Zincir Anlaşması (Global x1.8)", "Chain Agreement (Global x1.8)");
        AddText("Rota Optimizasyonu (Motorlu Kurye x2)", "Rota Optimizasyonu (Motorlu Kurye x2)", "Route Optimization (Motorcycle Courier x2)");
        AddText("Çift Hazne (Lavaş Makinesi x2)", "Çift Hazne (Lavaş Makinesi x2)", "Dual Hopper (Lavash Machine x2)");
        AddText("Bıçak Sanatı (Tıklama = üretimin %2.8'i)", "Bıçak Sanatı (Tıklama = üretimin %2.8'i)", "Art of the Knife (Click = 2.8% of production)");
        AddText("İlçe Zinciri (Global x1.3)", "İlçe Zinciri (Global x1.3)", "District Chain (Global x1.3)");
        AddText("Kör Tadım (Döner Ustası x2)", "Kör Tadım (Döner Ustası x2)", "Blind Tasting (Doner Master x2)");
        AddText("Liderlik Semineri (Şube Müdürü x2)", "Liderlik Semineri (Şube Müdürü x2)", "Leadership Seminar (Branch Manager x2)");
        AddText("Borsaya Açılma (Global x2.0)", "Borsaya Açılma (Global x2.0)", "Going Public (Global x2.0)");
        AddText("Otomatik Hamur (Lavaş Makinesi x2)", "Otomatik Hamur (Lavaş Makinesi x2)", "Auto Dough (Lavash Machine x2)");
        AddText("Kozmik Parmak (Tıklama = üretimin %4'i)", "Kozmik Parmak (Tıklama = üretimin %4'i)", "Cosmic Finger (Click = 4% of production)");
        AddText("Performans Primi (Şube Müdürü x2)", "Performans Primi (Şube Müdürü x2)", "Performance Bonus (Branch Manager x2)");
        AddText("Şehir Markası (Global x1.3)", "Şehir Markası (Global x1.3)", "City Brand (Global x1.3)");
        AddText("Çırak Sendikası (Stajyer Çırak x2)", "Çırak Sendikası (Stajyer Çırak x2)", "Apprentice Union (Intern Apprentice x2)");
        AddText("Gece Vardiyası (Motorlu Kurye x2)", "Gece Vardiyası (Motorlu Kurye x2)", "Night Shift (Motorcycle Courier x2)");
        AddText("Ortak Mutfak (Franchise Ağı x2)", "Ortak Mutfak (Franchise Ağı x2)", "Shared Kitchen (Franchise Network x2)");
        AddText("Mars Şubesi (Global x2.0)", "Mars Şubesi (Global x2.0)", "Mars Branch (Global x2.0)");
        AddText("Bölge Müdürlüğü (Şube Müdürü x2)", "Bölge Müdürlüğü (Şube Müdürü x2)", "Regional Directorate (Branch Manager x2)");
        AddText("Marka Kılavuzu (Franchise Ağı x2)", "Marka Kılavuzu (Franchise Ağı x2)", "Brand Guidelines (Franchise Network x2)");
        AddText("Bölge Devi (Global x1.35)", "Bölge Devi (Global x1.35)", "Regional Giant (Global x1.35)");
        AddText("Lazer Kesim (Otomatik Bıçak x2)", "Lazer Kesim (Otomatik Bıçak x2)", "Laser Cut (Auto Knife x2)");
        AddText("Döner Büyücüsü (Tıklama = üretimin %5.4'i)", "Döner Büyücüsü (Tıklama = üretimin %5.4'i)", "Doner Wizard (Click = 5.4% of production)");
        AddText("Sonsuz Un (Lavaş Makinesi x2)", "Sonsuz Un (Lavaş Makinesi x2)", "Infinite Flour (Lavash Machine x2)");
        AddText("Montaj Hattı (Döner Fabrikası x2)", "Montaj Hattı (Döner Fabrikası x2)", "Assembly Line (Doner Factory x2)");
        AddText("Galaktik Federasyon (Global x2.2)", "Galaktik Federasyon (Global x2.2)", "Galactic Federation (Global x2.2)");
        AddText("Ulusal Tedarik (Franchise Ağı x2)", "Ulusal Tedarik (Franchise Ağı x2)", "National Supply (Franchise Network x2)");
        AddText("Gece Üretimi (Döner Fabrikası x2)", "Gece Üretimi (Döner Fabrikası x2)", "Night Production (Doner Factory x2)");
        AddText("Ulusal Zincir (Global x1.35)", "Ulusal Zincir (Global x1.35)", "National Chain (Global x1.35)");
        AddText("Usta Loncası (Döner Ustası x2)", "Usta Loncası (Döner Ustası x2)", "Master Guild (Doner Master x2)");
        AddText("Kurumsal Kültür (Şube Müdürü x2)", "Kurumsal Kültür (Şube Müdürü x2)", "Corporate Culture (Branch Manager x2)");
        AddText("Soğuk Zincir (Merkez Depo x2)", "Soğuk Zincir (Merkez Depo x2)", "Cold Chain (Central Warehouse x2)");
        AddText("Tanrısal Parmak (Tıklama = üretimin %7'i)", "Tanrısal Parmak (Tıklama = üretimin %7'i)", "Godly Finger (Click = 7% of production)");
        AddText("Evrensel Barış (Global x2.2)", "Evrensel Barış (Global x2.2)", "Universal Peace (Global x2.2)");
        AddText("Robot Kollar (Döner Fabrikası x2)", "Robot Kollar (Döner Fabrikası x2)", "Robotic Arms (Doner Factory x2)");
        AddText("Otomatik Sayım (Merkez Depo x2)", "Otomatik Sayım (Merkez Depo x2)", "Auto Inventory (Central Warehouse x2)");
        AddText("Ustalık Yolu (Stajyer Çırak x2)", "Ustalık Yolu (Stajyer Çırak x2)", "Path of Mastery (Intern Apprentice x2)");
        AddText("Kıtalar Arası (Global x1.4)", "Kıtalar Arası (Global x1.4)", "Intercontinental (Global x1.4)");
        AddText("Kurye Filosu (Motorlu Kurye x2)", "Kurye Filosu (Motorlu Kurye x2)", "Courier Fleet (Motorcycle Courier x2)");
        AddText("Bayilik Ağı (Franchise Ağı x2)", "Bayilik Ağı (Franchise Ağı x2)", "Dealership Network (Franchise Network x2)");
        AddText("Tarif Öğrenmesi (Yapay Zeka Usta x2)", "Tarif Öğrenmesi (Yapay Zeka Usta x2)", "Recipe Learning (AI Chef x2)");
        AddText("Kader Parmağı (Tıklama = üretimin %8.8'i)", "Kader Parmağı (Tıklama = üretimin %8.8'i)", "Finger of Destiny (Click = 8.8% of production)");
        AddText("SONSUZLUK SOSU (Global x2.5)", "SONSUZLUK SOSU (Global x2.5)", "INFINITY SAUCE (Global x2.5)");
        AddText("Dev Silo (Merkez Depo x2)", "Dev Silo (Merkez Depo x2)", "Giant Silo (Central Warehouse x2)");
        AddText("Sinir Ağı v2 (Yapay Zeka Usta x2)", "Sinir Ağı v2 (Yapay Zeka Usta x2)", "Neural Network v2 (AI Chef x2)");
        AddText("Plazma Ağzı (Otomatik Bıçak x2)", "Plazma Ağzı (Otomatik Bıçak x2)", "Plasma Edge (Auto Knife x2)");
        AddText("Dünya Markası (Global x1.4)", "Dünya Markası (Global x1.4)", "World Brand (Global x1.4)");
        AddText("Lavaş Hattı (Lavaş Makinesi x2)", "Lavaş Hattı (Lavaş Makinesi x2)", "Lavash Line (Lavash Machine x2)");
        AddText("Sıfır Fire (Döner Fabrikası x2)", "Sıfır Fire (Döner Fabrikası x2)", "Zero Waste (Doner Factory x2)");
        AddText("İyon Motoru (Gezegenler Arası Kargo x2)", "İyon Motoru (Gezegenler Arası Kargo x2)", "Ion Engine (Interplanetary Cargo x2)");
        AddText("Multiverse Tıklaması (Tıklama = üretimin %10.8'i)", "Multiverse Tıklaması (Tıklama = üretimin %10.8'i)", "Multiverse Click (Click = 10.8% of production)");
        AddText("Kendi Kendine Öğrenme (Yapay Zeka Usta x2)", "Kendi Kendine Öğrenme (Yapay Zeka Usta x2)", "Self-Learning (AI Chef x2)");
        AddText("Solucan Deliği Rotası (Gezegenler Arası Kargo x2)", "Solucan Deliği Rotası (Gezegenler Arası Kargo x2)", "Wormhole Route (Interplanetary Cargo x2)");
        AddText("Şef Yıldızı (Döner Ustası x2)", "Şef Yıldızı (Döner Ustası x2)", "Chef Star (Doner Master x2)");
        AddText("Yıldızlar Arası (Global x1.5)", "Yıldızlar Arası (Global x1.5)", "Interstellar (Global x1.5)");
        AddText("Yönetim Kurulu (Şube Müdürü x2)", "Yönetim Kurulu (Şube Müdürü x2)", "Board of Directors (Branch Manager x2)");
        AddText("Akıllı Raf (Merkez Depo x2)", "Akıllı Raf (Merkez Depo x2)", "Smart Shelf (Central Warehouse x2)");
        AddText("Yerçekimsiz Mutfak (Yörünge Lokantası x2)", "Yerçekimsiz Mutfak (Yörünge Lokantası x2)", "Zero-G Kitchen (Orbital Restaurant x2)");
        AddText("Efsanevi Çırak (Stajyer Çırak x2)", "Efsanevi Çırak (Stajyer Çırak x2)", "Legendary Apprentice (Intern Apprentice x2)");
        AddText("Sonsuz El (Tıklama = üretimin %13'i)", "Sonsuz El (Tıklama = üretimin %13'i)", "Infinite Hand (Click = 13% of production)");
        AddText("Warp Konvoyu (Gezegenler Arası Kargo x2)", "Warp Konvoyu (Gezegenler Arası Kargo x2)", "Warp Convoy (Interplanetary Cargo x2)");
        AddText("Panoramik Salon (Yörünge Lokantası x2)", "Panoramik Salon (Yörünge Lokantası x2)", "Panoramic Hall (Orbital Restaurant x2)");
        AddText("Şehir Ağı (Motorlu Kurye x2)", "Şehir Ağı (Motorlu Kurye x2)", "City Network (Motorcycle Courier x2)");
        AddText("Galaktik Franchise (Global x1.5)", "Galaktik Franchise (Global x1.5)", "Galactic Franchise (Global x1.5)");
        AddText("Küresel Ağ (Franchise Ağı x2)", "Küresel Ağ (Franchise Ağı x2)", "Global Network (Franchise Network x2)");
        AddText("Duygusal Tat (Yapay Zeka Usta x2)", "Duygusal Tat (Yapay Zeka Usta x2)", "Emotional Taste (AI Chef x2)");
        AddText("Kararlı Alan (Kuantum Sentezleyici x2)", "Kararlı Alan (Kuantum Sentezleyici x2)", "Stable Field (Quantum Synthesizer x2)");
        AddText("Nano Keskinlik (Otomatik Bıçak x2)", "Nano Keskinlik (Otomatik Bıçak x2)", "Nano Sharpness (Auto Knife x2)");
        AddText("Meteor Izgarası (Yörünge Lokantası x2)", "Meteor Izgarası (Yörünge Lokantası x2)", "Meteor Grill (Orbital Restaurant x2)");
        AddText("Parçacık Hızlandırıcı (Kuantum Sentezleyici x2)", "Parçacık Hızlandırıcı (Kuantum Sentezleyici x2)", "Particle Accelerator (Quantum Synthesizer x2)");
        AddText("Endüstriyel Fırın (Lavaş Makinesi x2)", "Endüstriyel Fırın (Lavaş Makinesi x2)", "Industrial Oven (Lavash Machine x2)");
        AddText("Evrensel Tekel (Global x1.6)", "Evrensel Tekel (Global x1.6)", "Universal Monopoly (Global x1.6)");
        AddText("Karanlık Fabrika (Döner Fabrikası x2)", "Karanlık Fabrika (Döner Fabrikası x2)", "Dark Factory (Doner Factory x2)");
        AddText("Antimadde Yakıtı (Gezegenler Arası Kargo x2)", "Antimadde Yakıtı (Gezegenler Arası Kargo x2)", "Antimatter Fuel (Interplanetary Cargo x2)");
        AddText("Yıldız Ocağı (Yıldız Sistemi Zinciri x2)", "Yıldız Ocağı (Yıldız Sistemi Zinciri x2)", "Star Grill (Star System Chain x2)");
        AddText("Efsane Tarif (Döner Ustası x2)", "Efsane Tarif (Döner Ustası x2)", "Legendary Recipe (Doner Master x2)");
        AddText("Süperpozisyon Fırını (Kuantum Sentezleyici x2)", "Süperpozisyon Fırını (Kuantum Sentezleyici x2)", "Superposition Oven (Quantum Synthesizer x2)");
        AddText("Nötron Bıçağı (Yıldız Sistemi Zinciri x2)", "Nötron Bıçağı (Yıldız Sistemi Zinciri x2)", "Neutron Knife (Star System Chain x2)");
        AddText("Strateji Ofisi (Şube Müdürü x2)", "Strateji Ofisi (Şube Müdürü x2)", "Strategy Office (Branch Manager x2)");
        AddText("Anlık Sevkiyat (Merkez Depo x2)", "Anlık Sevkiyat (Merkez Depo x2)", "Instant Delivery (Central Warehouse x2)");
        AddText("Güneş Fırını (Yörünge Lokantası x2)", "Güneş Fırını (Yörünge Lokantası x2)", "Solar Oven (Orbital Restaurant x2)");
        AddText("Boyut Kapısı (Çoklu Evren Şubesi x2)", "Boyut Kapısı (Çoklu Evren Şubesi x2)", "Dimensional Gate (Multiverse Branch x2)");
        AddText("Çırak Ordusu (Stajyer Çırak x2)", "Çırak Ordusu (Stajyer Çırak x2)", "Apprentice Army (Intern Apprentice x2)");
        AddText("Drone Teslimat (Motorlu Kurye x2)", "Drone Teslimat (Motorlu Kurye x2)", "Drone Delivery (Motorcycle Courier x2)");
        AddText("Süpernova Marine (Yıldız Sistemi Zinciri x2)", "Süpernova Marine (Yıldız Sistemi Zinciri x2)", "Supernova Marinade (Star System Chain x2)");
        AddText("Paralel Mutfaklar (Çoklu Evren Şubesi x2)", "Paralel Mutfaklar (Çoklu Evren Şubesi x2)", "Parallel Kitchens (Multiverse Branch x2)");
        AddText("Marka Tescili (Franchise Ağı x2)", "Marka Tescili (Franchise Ağı x2)", "Trademark Registration (Franchise Network x2)");
        AddText("Yapay Bilinç (Yapay Zeka Usta x2)", "Yapay Bilinç (Yapay Zeka Usta x2)", "Artificial Consciousness (AI Chef x2)");
        AddText("Dolanık Tarif (Kuantum Sentezleyici x2)", "Dolanık Tarif (Kuantum Sentezleyici x2)", "Entangled Recipe (Quantum Synthesizer x2)");
        AddText("İlk Ateş (Sonsuzluk Ocağı x2)", "İlk Ateş (Sonsuzluk Ocağı x2)", "The First Fire (Infinity Grill x2)");
        AddText("Kendi Kendini Bileyen (Otomatik Bıçak x2)", "Kendi Kendini Bileyen (Otomatik Bıçak x2)", "Self-Sharpening (Auto Knife x2)");
        AddText("Hamur Zekası (Lavaş Makinesi x2)", "Hamur Zekası (Lavaş Makinesi x2)", "Dough Intelligence (Lavash Machine x2)");
        AddText("Sonsuz Şube (Çoklu Evren Şubesi x2)", "Sonsuz Şube (Çoklu Evren Şubesi x2)", "Infinite Branch (Multiverse Branch x2)");
        AddText("Zaman Dışı Fırın (Sonsuzluk Ocağı x2)", "Zaman Dışı Fırın (Sonsuzluk Ocağı x2)", "Timeless Oven (Infinity Grill x2)");
        AddText("Kendi Kendini Onaran (Döner Fabrikası x2)", "Kendi Kendini Onaran (Döner Fabrikası x2)", "Self-Repairing (Doner Factory x2)");
        AddText("Galaktik Lojistik (Gezegenler Arası Kargo x2)", "Galaktik Lojistik (Gezegenler Arası Kargo x2)", "Galactic Logistics (Interplanetary Cargo x2)");
        AddText("Kara Delik Fırını (Yıldız Sistemi Zinciri x2)", "Kara Delik Fırını (Yıldız Sistemi Zinciri x2)", "Black Hole Oven (Star System Chain x2)");
        AddText("Ustalar Ustası (Döner Ustası x2)", "Ustalar Ustası (Döner Ustası x2)", "Master of Masters (Doner Master x2)");
        AddText("Görünmez Patron (Şube Müdürü x2)", "Görünmez Patron (Şube Müdürü x2)", "Invisible Boss (Branch Manager x2)");
        AddText("Mutlak Tarif (Sonsuzluk Ocağı x2)", "Mutlak Tarif (Sonsuzluk Ocağı x2)", "Absolute Recipe (Infinity Grill x2)");
        AddText("Sonsuz Depo (Merkez Depo x2)", "Sonsuz Depo (Merkez Depo x2)", "Infinite Warehouse (Central Warehouse x2)");
        AddText("Yörünge Zinciri (Yörünge Lokantası x2)", "Yörünge Zinciri (Yörünge Lokantası x2)", "Orbital Chain (Orbital Restaurant x2)");
        AddText("Ayna Evren (Çoklu Evren Şubesi x2)", "Ayna Evren (Çoklu Evren Şubesi x2)", "Mirror Universe (Multiverse Branch x2)");
        AddText("Sonsuz Vardiya (Stajyer Çırak x2)", "Sonsuz Vardiya (Stajyer Çırak x2)", "Endless Shift (Intern Apprentice x2)");
        AddText("Anlık Sevkiyat (Motorlu Kurye x2)", "Anlık Sevkiyat (Motorlu Kurye x2)", "Instant Shipment (Motorcycle Courier x2)");
        AddText("Sokak Hakimiyeti (Franchise Ağı x2)", "Sokak Hakimiyeti (Franchise Ağı x2)", "Street Dominance (Franchise Network x2)");
        AddText("Süper Zeka (Yapay Zeka Usta x2)", "Süper Zeka (Yapay Zeka Usta x2)", "Super Intelligence (AI Chef x2)");
        AddText("Sıfır Nokta Enerjisi (Kuantum Sentezleyici x2)", "Sıfır Nokta Enerjisi (Kuantum Sentezleyici x2)", "Zero-Point Energy (Quantum Synthesizer x2)");
        AddText("Yaratılış Közü (Sonsuzluk Ocağı x2)", "Yaratılış Közü (Sonsuzluk Ocağı x2)", "Ember of Creation (Infinity Grill x2)");
        AddText("Kesmeyen Kesik (Otomatik Bıçak x2)", "Kesmeyen Kesik (Otomatik Bıçak x2)", "The Uncut Cut (Auto Knife x2)");
        AddText("Lavaş Şelalesi (Lavaş Makinesi x2)", "Lavaş Şelalesi (Lavaş Makinesi x2)", "Lavash Waterfall (Lavash Machine x2)");
        AddText("Tam Otomasyon (Döner Fabrikası x2)", "Tam Otomasyon (Döner Fabrikası x2)", "Full Automation (Doner Factory x2)");
        AddText("Yıldız Haritası (Gezegenler Arası Kargo x2)", "Yıldız Haritası (Gezegenler Arası Kargo x2)", "Star Map (Interplanetary Cargo x2)");
        AddText("Takımyıldız Menüsü (Yıldız Sistemi Zinciri x2)", "Takımyıldız Menüsü (Yıldız Sistemi Zinciri x2)", "Constellation Menu (Star System Chain x2)");
        AddText("Döner Piri (Döner Ustası x2)", "Döner Piri (Döner Ustası x2)", "Doner Sage (Doner Master x2)");
        AddText("Kendi Kendini Yöneten (Şube Müdürü x2)", "Kendi Kendini Yöneten (Şube Müdürü x2)", "Self-Managing (Branch Manager x2)");
        AddText("Bükülmüş Alan Deposu (Merkez Depo x2)", "Bükülmüş Alan Deposu (Merkez Depo x2)", "Warped Space Warehouse (Central Warehouse x2)");
        AddText("Halka İstasyon (Yörünge Lokantası x2)", "Halka İstasyon (Yörünge Lokantası x2)", "Ring Station (Orbital Restaurant x2)");
        AddText("Evren Tekeli (Çoklu Evren Şubesi x2)", "Evren Tekeli (Çoklu Evren Şubesi x2)", "Universe Monopoly (Multiverse Branch x2)");
        AddText("Zamansız Teslimat (Motorlu Kurye x2)", "Zamansız Teslimat (Motorlu Kurye x2)", "Timeless Delivery (Motorcycle Courier x2)");
        AddText("Her Köşede Şube (Franchise Ağı x2)", "Her Köşede Şube (Franchise Ağı x2)", "A Branch on Every Corner (Franchise Network x2)");
        AddText("Tekillik Mutfağı (Yapay Zeka Usta x2)", "Tekillik Mutfağı (Yapay Zeka Usta x2)", "Singularity Kitchen (AI Chef x2)");
        AddText("Kuantum Köpüğü (Kuantum Sentezleyici x2)", "Kuantum Köpüğü (Kuantum Sentezleyici x2)", "Quantum Foam (Quantum Synthesizer x2)");
        AddText("Sonsuzluk (Sonsuzluk Ocağı x2)", "Sonsuzluk (Sonsuzluk Ocağı x2)", "Infinity (Infinity Grill x2)");
        AddText("Ekmek Çağı (Lavaş Makinesi x2)", "Ekmek Çağı (Lavaş Makinesi x2)", "Age of Bread (Lavash Machine x2)");
        AddText("Fabrika Sürüsü (Döner Fabrikası x2)", "Fabrika Sürüsü (Döner Fabrikası x2)", "Factory Swarm (Doner Factory x2)");
        AddText("Işık Ötesi (Gezegenler Arası Kargo x2)", "Işık Ötesi (Gezegenler Arası Kargo x2)", "Faster-Than-Light (Interplanetary Cargo x2)");
        AddText("Galaktik Şölen (Yıldız Sistemi Zinciri x2)", "Galaktik Şölen (Yıldız Sistemi Zinciri x2)", "Galactic Feast (Star System Chain x2)");
        AddText("Mükemmel Hiyerarşi (Şube Müdürü x2)", "Mükemmel Hiyerarşi (Şube Müdürü x2)", "Perfect Hierarchy (Branch Manager x2)");
        AddText("Stok Kehaneti (Merkez Depo x2)", "Stok Kehaneti (Merkez Depo x2)", "Stock Prophecy (Central Warehouse x2)");
        AddText("Gezegen Manzarası (Yörünge Lokantası x2)", "Gezegen Manzarası (Yörünge Lokantası x2)", "Planetary View (Orbital Restaurant x2)");
        AddText("Boyutlar Arası Menü (Çoklu Evren Şubesi x2)", "Boyutlar Arası Menü (Çoklu Evren Şubesi x2)", "Interdimensional Menu (Multiverse Branch x2)");
        AddText("Franchise İmparatorluğu (Franchise Ağı x2)", "Franchise İmparatorluğu (Franchise Ağı x2)", "Franchise Empire (Franchise Network x2)");
        AddText("Dijital Usta (Yapay Zeka Usta x2)", "Dijital Usta (Yapay Zeka Usta x2)", "Digital Master (AI Chef x2)");
        AddText("Olasılık Motoru (Kuantum Sentezleyici x2)", "Olasılık Motoru (Kuantum Sentezleyici x2)", "Probability Engine (Quantum Synthesizer x2)");
        AddText("Başlangıç ve Son (Sonsuzluk Ocağı x2)", "Başlangıç ve Son (Sonsuzluk Ocağı x2)", "The Beginning and the End (Infinity Grill x2)");
        AddText("Üretim Tanrısı (Döner Fabrikası x2)", "Üretim Tanrısı (Döner Fabrikası x2)", "God of Production (Doner Factory x2)");
        AddText("Anlık Işınlama (Gezegenler Arası Kargo x2)", "Anlık Işınlama (Gezegenler Arası Kargo x2)", "Instant Teleportation (Interplanetary Cargo x2)");
        AddText("Yıldız Hasadı (Yıldız Sistemi Zinciri x2)", "Yıldız Hasadı (Yıldız Sistemi Zinciri x2)", "Star Harvest (Star System Chain x2)");
        AddText("Tükenmeyen Ambar (Merkez Depo x2)", "Tükenmeyen Ambar (Merkez Depo x2)", "Inexhaustible Warehouse (Central Warehouse x2)");
        AddText("Yörünge İmparatorluğu (Yörünge Lokantası x2)", "Yörünge İmparatorluğu (Yörünge Lokantası x2)", "Orbital Empire (Orbital Restaurant x2)");
        AddText("Sonsuz Olasılık (Çoklu Evren Şubesi x2)", "Sonsuz Olasılık (Çoklu Evren Şubesi x2)", "Infinite Possibility (Multiverse Branch x2)");
        AddText("Makine Rüyası (Yapay Zeka Usta x2)", "Makine Rüyası (Yapay Zeka Usta x2)", "Machine Dream (AI Chef x2)");
        AddText("Gerçeklik Düzenleyici (Kuantum Sentezleyici x2)", "Gerçeklik Düzenleyici (Kuantum Sentezleyici x2)", "Reality Editor (Quantum Synthesizer x2)");
        AddText("Var Oluş Sosu (Sonsuzluk Ocağı x2)", "Var Oluş Sosu (Sonsuzluk Ocağı x2)", "Sauce of Existence (Infinity Grill x2)");
        AddText("Uzay Kervanı (Gezegenler Arası Kargo x2)", "Uzay Kervanı (Gezegenler Arası Kargo x2)", "Space Caravan (Interplanetary Cargo x2)");
        AddText("Samanyolu Zinciri (Yıldız Sistemi Zinciri x2)", "Samanyolu Zinciri (Yıldız Sistemi Zinciri x2)", "Milky Way Chain (Star System Chain x2)");
        AddText("Gökyüzü Tekeli (Yörünge Lokantası x2)", "Gökyüzü Tekeli (Yörünge Lokantası x2)", "Sky Monopoly (Orbital Restaurant x2)");
        AddText("Her Evrende Şube (Çoklu Evren Şubesi x2)", "Her Evrende Şube (Çoklu Evren Şubesi x2)", "Branch in Every Universe (Multiverse Branch x2)");
        AddText("Mutlak Sentez (Kuantum Sentezleyici x2)", "Mutlak Sentez (Kuantum Sentezleyici x2)", "Absolute Synthesis (Quantum Synthesizer x2)");
        AddText("Tanrısal Ocak (Sonsuzluk Ocağı x2)", "Tanrısal Ocak (Sonsuzluk Ocağı x2)", "Divine Grill (Infinity Grill x2)");
        AddText("Kozmik Ziyafet (Yıldız Sistemi Zinciri x2)", "Kozmik Ziyafet (Yıldız Sistemi Zinciri x2)", "Cosmic Banquet (Star System Chain x2)");
        AddText("Çokluk Efendisi (Çoklu Evren Şubesi x2)", "Çokluk Efendisi (Çoklu Evren Şubesi x2)", "Lord of the Multiverse (Multiverse Branch x2)");
        AddText("SONSUZ DÖNER (Sonsuzluk Ocağı x2)", "SONSUZ DÖNER (Sonsuzluk Ocağı x2)", "INFINITE DONER (Infinity Grill x2)");


        // --- TAB (UI) SABİT METİNLERİ ---
        AddText("txt_tab_workers", "İŞÇİLER", "WORKERS");
        AddText("txt_tab_prestige", "PRESTİJ", "PRESTIGE");
        AddText("txt_settings_button", "AYARLAR", "SETTINGS");
        AddText("txt_tab_clicker", "KES", "CLICKER");
        AddText("txt_tab_upgrades", "GELİŞTİR", "UPGRADE");
        AddText("txt_tab_ads", "HIZLANDIR", "SPEED UP");

        // --- Panel (UI) SABİT METİNLERİ ---

    }

    private void AddText(string key, string trText, string enText)
    {
        var langDict = new Dictionary<Language, string>
        {
            { Language.Turkish, trText },
            { Language.English, enText }
        };
        localizedTexts.Add(key, langDict);
    }

    public string GetLocalizedValue(string key)
    {
        if (localizedTexts.ContainsKey(key))
        {
            return localizedTexts[key][currentLanguage];
        }
        return key; // Anahtar bulunamazsa hatayı fark etmek için anahtarı döndür
    }

    public void SetLanguage(Language newLanguage)
    {
        currentLanguage = newLanguage;
        // Dil değiştiğinde tüm dinleyicilere haber ver
        OnLanguageChanged?.Invoke(); 
    }
}