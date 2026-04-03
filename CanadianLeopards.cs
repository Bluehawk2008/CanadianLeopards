using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using MelonLoader;
using GHPC;
using GHPC.Camera;
using GHPC.Mission;
using GHPC.AI.Platoons;
using GHPC.State;
using GHPC.Vehicle;
using GHPC.Weapons;
using GHPC.Equipment.Optics;
using Reticle;
using GHPC.Effects.Voices;


namespace CanadianLeopards
{
    public class AlreadyConverted : MonoBehaviour
    {
        void Awake()
        {
            enabled = false;
        }
    }
    public class CanadianLeopardsClass : MelonMod
    {
        public static GameObject gameManager;
        public static MelonPreferences_Entry<string> ammo_loadout;
        public static MelonPreferences_Entry<bool> carc_green;
        public static MelonPreferences_Entry<bool> no_threecolour;
        public static MelonPreferences_Entry<bool> decals_outlined;
        public static MelonPreferences_Entry<bool> additional_decals;
        public static MelonPreferences_Entry<bool> mute_logger;

        static GameObject american_crew_voice;
        static bool activeScene = false;
        static Vehicle abrams = null;
        public override void OnInitializeMelon()
        {
            MelonPreferences_Category cfg = MelonPreferences.CreateCategory("Canadian Leopards");
            ammo_loadout = cfg.CreateEntry<string>("Customize ammo loadout", "historical");
            ammo_loadout.Comment = "'historical' for DM-23/13 and HESH, 'American' for M774 and HEAT, 'German' to keep mission defaults";

            no_threecolour = cfg.CreateEntry<bool>("Force single colour paint schemes", false);
            no_threecolour.Comment = "Prevents C1s from appearing in NATO three-colour camo";

            carc_green = cfg.CreateEntry<bool>("NATO/CARC Green", true);
            carc_green.Comment = "Replaces the default German Gelboliv ('yellow-olive') for NATO CARC, a brighter shade of green.";

            decals_outlined = cfg.CreateEntry<bool>("Decals outlined in white", true);
            decals_outlined.Comment = "Turret numbers and maple leaves have white borders when true; plain black when false.";

            additional_decals = cfg.CreateEntry<bool>("Additional decals", true);
            additional_decals.Comment = "Adds tactical unit symbol and MLC number to the hull.";

            mute_logger = cfg.CreateEntry<bool>("Mute log messages", false);
            mute_logger.Comment = "Mutes log messages in the MelonLoader console.";
        }
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu2_Scene" || sceneName == "t64_menu" || sceneName == "MainMenu2-1_Scene") 
            {
                activeScene = false;
                abrams = null;
                return; 
            }            

            gameManager = GameObject.Find("_APP_GHPC_");
            if (gameManager == null) { return; }            

            StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(Conversion), GameStatePriority.Medium);
        }

        public IEnumerator Conversion(GameState _)
        {
            if (activeScene == true) { yield break; }
            activeScene = true;
            Vehicle[] list = GameObject.FindObjectsByType<Vehicle>(FindObjectsSortMode.None);            
            
            foreach (var vehicle in list)
            {
                if (vehicle.UniqueName == "M1" && abrams == null)
                {
                    abrams = vehicle;
                    if (!mute_logger.Value) { MelonLogger.Msg("Abrams found in scene"); }
                }                
            }

            if (abrams == null)
            {
                var prefabLookups = UnityEngine.Object.FindAnyObjectByType<UnitSpawner>().PrefabLookup;
                AssetReference prefab = prefabLookups.GetPrefab("M1");
                var dummy_abrams = Addressables.LoadAssetAsync<GameObject>(prefab).WaitForCompletion();
                GameObject new_abrams = GameObject.Instantiate(dummy_abrams, new Vector3(100f, 5f, 100f), new Quaternion(0f, 0f, 0f, 0f));
                if (!mute_logger.Value) { MelonLogger.Msg("Dummy Abrams spawned"); }
                new_abrams.GetComponent<Vehicle>().Allegiance = Faction.Neutral;
                abrams = new_abrams.GetComponent<Vehicle>();
                new_abrams.gameObject.SetActive(false);
            }

            Texture2D maple = new Texture2D(128, 128);  //loading these images outside the main loop to avoid slowdowns
            string maplePath;
            if (decals_outlined.Value) { maplePath = "Mods/CanadianLeopards/maple.png"; }
            else { maplePath = "Mods/CanadianLeopards/maple_black.png"; }
            byte[] maple_data = File.ReadAllBytes(maplePath);
            if (maple_data != null) { maple.LoadImage(maple_data, true); }
            else { MelonLogger.Msg("Wanted texture file at " + maplePath + " missing!"); }

            Texture2D A1_base = new Texture2D(2048, 2048);
            string A1_basePath;
            if (carc_green.Value == true) { A1_basePath = "Mods/CanadianLeopards/green.png"; }
            else { A1_basePath = "Mods/CanadianLeopards/1A1_base.png"; }
            byte[] A1_base_data = File.ReadAllBytes(A1_basePath);
            if (A1_base_data != null) { A1_base.LoadImage(A1_base_data, true); }
            else { MelonLogger.Msg("Wanted texture file at " + A1_basePath + " missing!"); }

            Texture2D A3_base = new Texture2D(2048, 2048);
            string A3_basePath = "Mods/CanadianLeopards/1A3_base.png";
            byte[] A3_baseData = File.ReadAllBytes(A3_basePath);
            if (A3_baseData != null) { A3_base.LoadImage(A3_baseData, true); }
            else { MelonLogger.Msg("Wanted texture file at " + A3_basePath + " missing!"); }            

            Texture2D callsigns = new Texture2D(512, 64);
            string callsignsPath;
            if (decals_outlined.Value) { callsignsPath = "Mods/CanadianLeopards/callsigns.png"; }
            else { callsignsPath = "Mods/CanadianLeopards/callsigns_black.png"; }
            byte[] callsignsData = File.ReadAllBytes(callsignsPath);
            if (callsignsData != null) { callsigns.LoadImage(callsignsData, true); }
            else { MelonLogger.Msg("Wanted texture file at " + callsignsPath + " missing!"); }

            Texture2D A1_camomask = new Texture2D(2048, 2048);
            string camoPath;
            if (no_threecolour.Value) { camoPath = "Mods/CanadianLeopards/nocamMask.png"; }
            else { camoPath = "Mods/CanadianLeopards/A1_mask.png"; }
            byte[] camoData = File.ReadAllBytes(camoPath);
            if (camoData != null) { A1_camomask.LoadImage(camoData, true); } 
            else { MelonLogger.Msg("Wanted texture file at " + camoPath + " missing!"); }

            Texture2D A3_camomask = new Texture2D(2048, 2048);
            string camoPath3;
            if (no_threecolour.Value) { camoPath3 = "Mods/CanadianLeopards/nocamMask.png"; }
            else { camoPath3 = "Mods/CanadianLeopards/A3_mask.png"; }
            byte[] camoData3 = File.ReadAllBytes(camoPath3);
            if (camoData3 != null) { A3_camomask.LoadImage(camoData3, true); }
            else { MelonLogger.Msg("Wanted texture fiel at " + camoPath3 + " missing!"); }

            Texture2D nocamMask = new Texture2D(2048, 2048);
            string nocamMaskPath = "Mods/CanadianLeopards/nocamMask.png";
            byte[] nocamData = File.ReadAllBytes(nocamMaskPath);
            if (nocamData != null) { nocamMask.LoadImage(nocamData, true); }
            else { MelonLogger.Msg("Wanted texture file at " + nocamMaskPath + " missing!"); }

            Texture2D tac = new Texture2D(128, 98);
            string tacPath = "Mods/CanadianLeopards/tac.png";
            byte[] tacData = File.ReadAllBytes(tacPath);
            if (tacData != null) { tac.LoadImage(tacData, true); }
            else { MelonLogger.Msg("Wanted texture file at " + tacPath + " missing!"); }

            Texture2D mlc = new Texture2D(128, 128);
            string mlcPath;
            if (decals_outlined.Value) { mlcPath = "Mods/CanadianLeopards/mlc.png"; }
            else { mlcPath = "Mods/CanadianLeopards/mlc_black.png"; }
            byte[] mlcData = File.ReadAllBytes(mlcPath);
            if (mlcData != null) { mlc.LoadImage(mlcData, true); }
            else { MelonLogger.Msg("Wanted texture file at " + mlcPath + " missing!"); }

            foreach (var vehicle in list)
            {
                GameObject vehicle_go = vehicle.gameObject;
                if (vehicle_go == null) { continue; }
                if (vehicle_go.GetComponent<AlreadyConverted>() != null) { continue; }
                string short_name = vehicle_go.name.Substring(0, 3);
                if (short_name != "LEO") { continue; }
                vehicle_go.AddComponent<AlreadyConverted>();
                if (!mute_logger.Value) { MelonLogger.Msg("Found vic named: " + vehicle_go.name); }
                vehicle._friendlyName = "Leopard C1";  //New display name
                bool leo1a3 = false;
                short_name = vehicle_go.name.Substring(0, 6);
                if (short_name == "LEO1A3") { leo1a3 = true; }

                american_crew_voice = abrams.GetComponentInChildren<CrewVoiceHandler>().gameObject;        //Adding US Voices
                vehicle.transform.Find("DE Tank Voice").gameObject.SetActive(false);
                GameObject new_voice = GameObject.Instantiate(american_crew_voice, vehicle.transform);
                new_voice.transform.localPosition = new Vector3(0, 0, 0);
                new_voice.transform.localEulerAngles = new Vector3(0, 0, 0);
                CrewVoiceHandler handler = new_voice.GetComponent<CrewVoiceHandler>();
                handler._chassis = vehicle._chassis as NwhChassis;
                vehicle._crewVoiceHandler = handler;
                new_voice.SetActive(true);                

                WeaponSystem maingun = vehicle.GetComponent<WeaponsManager>().Weapons[0].Weapon;
                WeaponSystemInfo coax = vehicle.GetComponent<WeaponsManager>().Weapons[1];
                FireControlSystem fcs = vehicle.GetComponentInChildren<FireControlSystem>();                

                //Adding Laser-Range Finder and Lead-Calculator
                GameObject lrf_holder = new GameObject("Laser Rangefinder");
                lrf_holder.transform.SetParent(vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/--Turret Scripts--/Sights/GPS"));
                if (leo1a3) { lrf_holder.transform.localPosition = new Vector3(0f, 0f, 0.5f); }
                else { lrf_holder.transform.localPosition = new Vector3(0f, 0f, 0.2f); }
                lrf_holder.transform.localRotation = Quaternion.identity;
                GHPC.Equipment.DestructibleComponent laser_dest = lrf_holder.AddComponent<GHPC.Equipment.DestructibleComponent>();
                laser_dest._health = 5f;
                laser_dest._fullHealth = 5f;
                laser_dest._pressureTolerance = 1f;
                laser_dest._shockResistance = 0.30f;
                laser_dest._name = "Laser Rangefinder";

                fcs.LaserAim = LaserAimMode.ImpactPoint;
                fcs.LaserComponent = laser_dest;
                fcs.LaserOrigin = lrf_holder.transform;
                fcs.MaxLaserRange = 4000f;
                fcs.DynamicLead = true;
                fcs.SuperleadWeapon = true;
                fcs.SuperelevateWeapon = true;
                fcs.TraverseBufferSeconds = 0.5f;
                fcs._autoDumpViaPalmSwitches = true;
                fcs._originalSuperleadMode = true;
                fcs.ComputerNeedsPower = true;
                fcs.RecordTraverseRateBuffer = true;
                //fcs._manualModeOnRangeSet = true;
                //fcs._autoModeOnLase = true;
                UsableOptic sabca = fcs.MainOptic;
                sabca.ForceHorizontalReticleAlign = true;
                //sabca.RotateAzimuth = true;                

                UnityEngine.Object.Destroy(fcs.OpticalRangefinder);
                GameObject sabca_go = sabca.gameObject;
                CameraSlot sabca_cam = sabca_go.GetComponent<CameraSlot>();                

                //Ensuring PZB-200 Night Sight
                if (fcs.NightOptic == null)
                {
                    GameObject pzb_go;
                    GameObject aux_go;
                    if (leo1a3) 
                    { 
                        pzb_go = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/1A3 mantlet/--Gun Scripts--/PZB-200").gameObject;
                        aux_go = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/1A3 mantlet/--Gun Scripts--/Aux sight TZF1A").gameObject;
                    }
                    else 
                    { 
                        pzb_go = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/Mantlet/--Gun Scripts--/PZB-200").gameObject;
                        aux_go = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/Mantlet/--Gun Scripts--/Aux sight TZF1A").gameObject;
                    }
                    pzb_go.SetActive(true);
                    UsableOptic pzb = pzb_go.GetComponent<UsableOptic>();                    
                    pzb.ReticleActive = true;
                    pzb.StabsActive = true;
                    CameraSlot pzb_cam = pzb_go.GetComponent<CameraSlot>();
                    CameraSlot aux_cam = aux_go.GetComponent<CameraSlot>();
                    pzb_cam.LinkedDaySight = sabca_cam;
                    sabca_cam.LinkedNightSight = pzb_cam;
                    aux_cam.LinkedNightSight = pzb_cam;
                    pzb_cam._pairedOptic = pzb;
                    pzb_cam.IsLinkedNightSight = true;
                    pzb_cam._isUsableByWeapon = true;
                    pzb_cam.NightSightAtNightOnly = false;
                    fcs.NightOptic = pzb;
                    fcs.RegisterOptic(pzb);                    
                    if (leo1a3) { vehicle.transform.Find("LEO1A3_mesh/1A3_PZB200").gameObject.SetActive(true); }
                    else { vehicle.transform.Find("LEO1A1_mesh/PZB 200").gameObject.SetActive(true); }
                    if (!mute_logger.Value) { MelonLogger.Msg("Swapping night sights"); }
                }

                //Changing the reticle in the primary sight
                GameObject reticle_mesh_go = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/--Turret Scripts--/Sights/GPS/Reticle Mesh").gameObject;
                ReticleMesh reticle_mesh = reticle_mesh_go.GetComponent<ReticleMesh>();
                reticle_mesh.reticleSO = ReticleMesh.cachedReticles["TZF"].tree;
                reticle_mesh.reticle = ReticleMesh.cachedReticles["TZF"];
                reticle_mesh.SMR = null;
                reticle_mesh.Load();
                reticle_mesh.enabled = false;
                reticle_mesh.transform.localPosition = new Vector3(0.85f, -35.8f, 0f);
                reticle_mesh.transform.localRotation = reticle_mesh.transform.localRotation * Quaternion.Euler(new Vector3(0f, 0f, 1f));
                ReticleTree.Light new_light = new ReticleTree.Light();
                new_light.color = new RGB(4f, 3f, 0, true);
                new_light.type = ReticleTree.Light.Type.Powered;
                reticle_mesh.lights[0].light = new_light;
                reticle_mesh.lightCols[1] = new Vector4(4f, 3f, 0f, 1f);
                Mesh tzf = reticle_mesh_go.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                Vector3[] vertices = tzf.vertices;
                for (int i = 0; i < 2296; i++) //removing range scales and pointers but keeping central reticle
                {
                    if (i < 1872 || i > 2090)
                    {
                        vertices[i] = new Vector3(0f, 0f, 0f);
                    }
                }
                tzf.vertices = vertices;

                sabca_cam.DefaultFov = 9.52f;
                sabca_cam.OtherFovs = new float[] { 3f };
                sabca_cam.AllowFreeZoom = true;
                sabca_cam.ZoomInAudioEvent = "event:/Effects/Optic/Optic_Zoom_In";
                sabca_cam.ZoomOutAudioEvent = "event:/Effects/Optic/Optic_Zoom_Out";
                GameObject old_scale = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/--Turret Scripts--/Sights/GPS/Leopard 1 GPS canvas/distance scale").gameObject;
                old_scale.SetActive(false);

                maingun.WeaponData.FriendlyName = "105mm Gun L7A4 L/52";

                //Replacing Coax MG
                coax.Name = "7.62mm machine gun C6";
                AmmoFeed coax_ammo = coax.Weapon.Feed;
                coax_ammo._totalCycleTime = 0.08f;
                coax.Weapon.WeaponSound.LoopEventPath = "event:/Weapons/MG_m240_750rmp";                

                //Replacing loader-hatch MG               
                GameObject m240_prefab = abrams.transform.Find("IPM1_rig/HULL/TURRET/Turret Scripts/M240_loader").gameObject;
                Transform loader_station;
                if (leo1a3) { loader_station = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/lafette002"); }
                else { loader_station = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/lafette001"); }                
                GameObject loader_C6 = GameObject.Instantiate(m240_prefab, loader_station);
                Transform loader_MG3;
                if (leo1a3) { loader_MG3 = loader_station.transform.Find("MG004"); }
                else { loader_MG3 = loader_station.transform.Find("MG3"); }
                Transform MG3_box;
                if (leo1a3) { MG3_box = loader_station.transform.Find("MGbox002"); }
                else { MG3_box = loader_station.transform.Find("MGbox001"); }
                loader_C6.transform.localPosition = loader_MG3.localPosition + new Vector3(0f, 0f, 0.15f);
                loader_C6.transform.localRotation = loader_MG3.localRotation;
                loader_C6.transform.localEulerAngles = new Vector3(-90f, 90f, 90f);
                loader_MG3.gameObject.SetActive(false);
                MG3_box.gameObject.SetActive(false);
                MeshFilter old_pintle = loader_station.gameObject.GetComponent<MeshFilter>();
                old_pintle.mesh = null;                

                //Configuring Ammunition                
                if (ammo_loadout.Value != "German" || ammo_loadout.Value != "german")
                {
                    AmmoSwaps ammo_swaps = new AmmoSwaps();
                    LoadoutManager loadout_manager = vehicle.GetComponent<LoadoutManager>();

                    if (ammo_loadout.Value == "historical") { ammo_swaps.HistoricalLoad(maingun, loadout_manager, mute_logger.Value); }
                    else if (ammo_loadout.Value == "American" || ammo_loadout.Value == "american") { ammo_swaps.AmericanLoad(maingun, loadout_manager, mute_logger.Value); }
                    else
                    {
                        if (!mute_logger.Value) { MelonLogger.Msg("Unknown value for ammo loadout, using mission defaults"); }
                    }

                }

                //Texture cosmetics
                GameObject de_markings;
                if (leo1a3) { de_markings = vehicle.transform.Find("LEO1A3_markings").gameObject; }
                else { de_markings = vehicle.transform.Find("LEO1A1_markings").gameObject; }
                de_markings.SetActive(false);                
                GameObject cross = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/kreuz").gameObject;
                Material cross_mat = cross.GetComponent<MeshRenderer>().material;
                cross.SetActive(false);               

                GameObject active_hull;
                MeshRenderer base_mr;
                if (leo1a3)
                {
                    active_hull = vehicle.transform.Find("LEO1A3_mesh/1A3_hull").gameObject;
                }
                else
                {
                    GameObject hull_early = vehicle.transform.Find("LEO1A1_mesh/hull_early").gameObject;
                    GameObject hull_mid = vehicle.transform.Find("LEO1A1_mesh/hull_mid").gameObject;
                    GameObject hull_late = vehicle.transform.Find("LEO1A1_mesh/hull_late").gameObject;
                    if (hull_early.activeSelf == true)
                    {
                        active_hull = hull_early;
                    }
                    else if (hull_mid.activeSelf == true)
                    {
                        active_hull = hull_mid;
                    }
                    else
                    {
                        active_hull = hull_late;
                    }
                }
                base_mr = active_hull.GetComponent<MeshRenderer>();
                if (leo1a3) { base_mr.material.SetTexture("_Albedo", A3_base); }
                else { base_mr.material.SetTexture("_Albedo", A1_base); }                              
                if (leo1a3) { base_mr.material.SetTexture("_PaintMask", A3_camomask); }
                else { base_mr.material.SetTexture("_PaintMask", A1_camomask); }                

                if (leo1a3)
                {   
                    GameObject a3_turret = vehicle.transform.Find("LEO1A3_mesh/A3 turret").gameObject;
                    GameObject a3_skirt_cut = vehicle.transform.Find("LEO1A3_mesh/a3_skirt_cut").gameObject;
                    GameObject a3_skirt_full = vehicle.transform.Find("LEO1A3_mesh/a3_skirt_full").gameObject;
                    GameObject a3_wheels = vehicle.transform.Find("LEO1A3_mesh/running gear").gameObject;
                    a3_turret.GetComponent<SkinnedMeshRenderer>().material = base_mr.material;
                    a3_skirt_cut.GetComponent<MeshRenderer>().material = base_mr.material;
                    a3_skirt_full.GetComponent<MeshRenderer>().material = base_mr.material;
                    a3_wheels.GetComponent<SkinnedMeshRenderer>().material = base_mr.material;
                    if (!mute_logger.Value) { MelonLogger.Msg("Vehicle repainted"); }
                }
                else
                {                   
                        
                    GameObject turret_early = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/turret_early").gameObject;
                    GameObject turret_late = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/turret_late").gameObject;
                    GameObject gun_barrel = vehicle.transform.Find("LEO1A1_mesh/gun").gameObject;
                    GameObject side_skirts = vehicle.transform.Find("LEO1A1_mesh/side skirts").gameObject;
                    GameObject skirt_full = vehicle.transform.Find("LEO1A1_mesh/skirt_full").gameObject;
                    GameObject skirts_cut0 = vehicle.transform.Find("LEO1A1_mesh/skirts_cut0").gameObject;
                    GameObject skirts_cut1 = vehicle.transform.Find("LEO1A1_mesh/skirts_cut1").gameObject;
                    GameObject skirts_cut2 = vehicle.transform.Find("LEO1A1_mesh/skirts_cut2").gameObject;
                    GameObject wheels = vehicle.transform.Find("LEO1A1_mesh/running gear").gameObject;
                    turret_early.GetComponent<MeshRenderer>().material = base_mr.material;
                    turret_late.GetComponent<MeshRenderer>().material = base_mr.material;
                    gun_barrel.GetComponent<SkinnedMeshRenderer>().material = base_mr.material;
                    side_skirts.GetComponent<MeshRenderer>().material = base_mr.material;
                    skirt_full.GetComponent<MeshRenderer>().material = base_mr.material;
                    skirts_cut0.GetComponent<MeshRenderer>().material = base_mr.material;
                    skirts_cut1.GetComponent<MeshRenderer>().material = base_mr.material;
                    skirts_cut2.GetComponent<MeshRenderer>().material = base_mr.material;
                    wheels.GetComponent<SkinnedMeshRenderer>().material = base_mr.material;
                    if (!mute_logger.Value) { MelonLogger.Msg("Vehicle repainted"); }
                    
                }
                
                //The Iron-Cross decals have weird UVs so we need to create custom meshes
                GameObject turret = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET").gameObject;
                GameObject maple_left = new GameObject("Mapleleaf_left");
                maple_left.transform.parent = turret.transform;
                maple_left.AddComponent<MeshFilter>();
                maple_left.AddComponent<MeshRenderer>();
                maple_left.GetComponent<MeshFilter>().mesh = new Mesh();
                maple_left.GetComponent<MeshFilter>().mesh.vertices = new Vector3[] {
                        new Vector3(0.165f, 0 , 0.165f), new Vector3(0.165f, 0, -0.165f),
                        new Vector3(-0.165f, 0, 0.165f), new Vector3(-0.165f, 0, -0.165f) };
                maple_left.GetComponent<MeshFilter>().mesh.uv = new Vector2[] {
                        new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, 0) };
                maple_left.GetComponent<MeshFilter>().mesh.triangles = new int[] { 0, 1, 2, 2, 1, 3 };
                maple_left.GetComponent<MeshRenderer>().material = cross_mat;
                maple_left.GetComponent<MeshRenderer>().material.mainTexture = maple;
                maple_left.transform.position = turret.transform.position;
                if (leo1a3)
                {
                    maple_left.transform.localPosition += new Vector3(-1.15f, 0.628f, 0.08f);
                    maple_left.transform.rotation = turret.transform.rotation * Quaternion.Euler(new Vector3(3f, 0f, 63f));
                    maple_left.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                }
                else
                {
                    maple_left.transform.localPosition += new Vector3(-1.135f, 0.6f, -0.15f);
                    maple_left.transform.rotation = turret.transform.rotation * Quaternion.Euler(new Vector3(0f, 10f, 60f));                    
                }
                maple_left.GetComponent<MeshFilter>().mesh.RecalculateNormals();

                GameObject maple_right = new GameObject("Mapleleaf_right");
                maple_right.transform.parent = turret.transform;
                maple_right.AddComponent<MeshFilter>();
                maple_right.AddComponent<MeshRenderer>();
                maple_right.GetComponent<MeshFilter>().mesh = new Mesh();
                maple_right.GetComponent<MeshFilter>().mesh.vertices = new Vector3[] {
                        new Vector3(0.165f, 0 , 0.165f), new Vector3(0.165f, 0, -0.165f),
                        new Vector3(-0.165f, 0, 0.165f), new Vector3(-0.165f, 0, -0.165f) };
                maple_right.GetComponent<MeshFilter>().mesh.uv = new Vector2[] {
                        new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, 0) };
                maple_right.GetComponent<MeshFilter>().mesh.triangles = new int[] { 0, 1, 2, 2, 1, 3 };
                maple_right.GetComponent<MeshRenderer>().material = cross_mat;
                maple_right.GetComponent<MeshRenderer>().material.mainTexture = maple;
                maple_right.transform.position = turret.transform.position;
                if (leo1a3) 
                {
                    maple_right.transform.localPosition += new Vector3(1.15f, 0.628f, 0.1f);
                    maple_right.transform.rotation = turret.transform.rotation * Quaternion.Euler(new Vector3(0f, 180f, 62f));
                    maple_right.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                }
                else 
                { 
                    maple_right.transform.localPosition += new Vector3(1.135f, 0.6f, -0.15f);
                    maple_right.transform.rotation = turret.transform.rotation * Quaternion.Euler(new Vector3(0f, 170f, 60f));
                }
                maple_right.GetComponent<MeshFilter>().mesh.RecalculateNormals();                

                //Turret numbers: Company [1-4], Troop [1-4], Vic [blank, A B C]
                PlatoonData platoon = vehicle.Platoon;
                int position_in_platoon = 0;
                if (vehicle.Platoon == null) { position_in_platoon = UnityEngine.Random.Range(0, 4); }
                else
                {
                    int platoon_size = vehicle.Platoon.Units.Count;
                    for (int i = 0; i < platoon_size; i++)
                    {
                        if (platoon.Units[i] == vehicle) { position_in_platoon = i; } //0 is first ... 3 is fourth
                    }
                }
                MergedVehicleNumberControl[] components = de_markings.GetComponents<MergedVehicleNumberControl>();
                MergedVehicleNumberControl turret_numbers = new MergedVehicleNumberControl();
                foreach (var mvnc in components)
                {
                    if (mvnc.Type == VehicleDecalType.UnitNumber) { turret_numbers = mvnc; }
                }
                if (turret_numbers._allValues[0] == 9) { turret_numbers._allValues[0] = 1; }
                else if (turret_numbers._allValues[0] > 4) { turret_numbers._allValues[0] -= 4; } //5-8 get remapped onto 1-4
                if (turret_numbers._allValues[1] == 9) { turret_numbers._allValues[1] = 1; }
                else if (turret_numbers._allValues[1] > 4) { turret_numbers._allValues[1] -= 4; } 
                else if (turret_numbers._allValues[1] == 0) { turret_numbers._allValues[1] = 1; }
                turret_numbers._allValues[2] = position_in_platoon + 6; // 6-9 in the texture will become letter codes
                turret_numbers.RefreshDecals();
                
                GameObject numbers_go;
                if (leo1a3) { numbers_go = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/numbers").gameObject; }
                else { numbers_go = vehicle.transform.Find("LEO1A1A1_rig/HULL/TURRET/NUMBERS").gameObject; }
                MeshRenderer numbers = numbers_go.GetComponent<MeshRenderer>();
                numbers.material.mainTexture = callsigns;                

                if (leo1a3) {
                    numbers_go.transform.localPosition = new Vector3(-0.42f, 0.55f, -1.245f);  //moving turret numbers to back of the turret
                    numbers_go.transform.localRotation = Quaternion.Euler(270f, 270f, 0f);
                    numbers_go.transform.localScale = new Vector3(1f, 0.85f, 1f);
                }
                else 
                {
                    numbers_go.transform.localPosition = new Vector3(1.115f, 0.31f, -1.345f); 
                    numbers_go.transform.localRotation = numbers_go.transform.localRotation * Quaternion.Euler(0f, 90f, 7f);
                    numbers_go.transform.localScale = new Vector3(0.48f, 0.48f, 0.4f);
                }
                
                Vector3[] num_vertices = numbers_go.GetComponent<MeshFilter>().sharedMesh.vertices;
                if (leo1a3) 
                {
                    for (int i = 0; i < 12; i++)
                    {
                        num_vertices[i] = new Vector3(0f, 0f, 0f);
                    }
                    num_vertices[14] += new Vector3(0.13f, 0f, 0f); //changing angle of decal to play nice with the back of the turret
                    num_vertices[15] += new Vector3(0.13f, 0f, 0f);
                    num_vertices[18] += new Vector3(0.13f, 0f, 0f);
                    num_vertices[19] += new Vector3(0.13f, 0f, 0f);
                    num_vertices[22] += new Vector3(0.13f, 0f, 0f);
                    num_vertices[23] += new Vector3(0.13f, 0f, 0f);
                }
                else 
                { 
                    for (int i = 0; i < 21; i++)
                    {
                        num_vertices[i] = new Vector3(0f, 0f, 0f); //removing unneeded double image
                    }  
                    num_vertices[26] += new Vector3(-0.03f, 0f, 0f); //flattening out the bottom-right corner
                    num_vertices[28] += new Vector3(-0.03f, 0f, 0f);
                    num_vertices[29] += new Vector3(-0.03f, 0f, 0f);                    
                }
                numbers_go.GetComponent<MeshFilter>().sharedMesh.vertices = num_vertices;
                
                GameObject hull_numbers = new GameObject("hull callsign"); //new number decal for the back of the hull
                hull_numbers.transform.parent = active_hull.transform;
                hull_numbers.AddComponent<MeshFilter>();
                hull_numbers.AddComponent<MeshRenderer>();
                hull_numbers.GetComponent<MeshRenderer>().material = numbers.material;
                hull_numbers.GetComponent<MeshFilter>().mesh = numbers_go.GetComponent<MeshFilter>().mesh;
                hull_numbers.transform.position = active_hull.transform.position;
                if (leo1a3) 
                {
                    hull_numbers.transform.localPosition += new Vector3(-0.6f, 4.35f, 1.6f);
                    hull_numbers.transform.localRotation = Quaternion.Euler(355f, 0f, 270f);
                    hull_numbers.transform.localScale = new Vector3(1f, 1f, 1f);
                } 
                else 
                {                 
                    hull_numbers.transform.localPosition += new Vector3(1.88f, 3.12f, -8.9f);
                    hull_numbers.transform.localRotation = Quaternion.Euler(0f, 89f, -16f);
                    hull_numbers.transform.localScale = new Vector3(1f, 1f, 1.1f);
                }

                if (position_in_platoon == 0) //centers decals for troop-leaders
                {
                    if (leo1a3) 
                    {
                        numbers_go.transform.localPosition += new Vector3(0.105f, 0f, 0f);
                        hull_numbers.transform.localPosition += new Vector3(0.1f, 0f, 0f);
                    }
                    else 
                    { 
                        numbers_go.transform.localPosition += new Vector3(0.07f, 0f, 0f);
                        hull_numbers.transform.localPosition += new Vector3(0.2f, 0f, 0f);
                    }
                }

                if (additional_decals.Value) { 
                    //NATO map symbol type decal, front and back
                    GameObject hull_tac_front = new GameObject("tactical sign front"); 
                    hull_tac_front.transform.parent = active_hull.transform;
                    hull_tac_front.AddComponent<MeshFilter>();
                    hull_tac_front.AddComponent<MeshRenderer>();
                    hull_tac_front.GetComponent<MeshRenderer>().material = numbers.material;
                    hull_tac_front.GetComponent<MeshRenderer>().material.mainTexture = tac;
                    hull_tac_front.GetComponent<MeshFilter>().mesh = new Mesh();
                    hull_tac_front.GetComponent<MeshFilter>().mesh.vertices = new Vector3[] {
                            new Vector3(0.165f, 0 , 0.165f), new Vector3(0.165f, 0, -0.165f),
                            new Vector3(-0.165f, 0, 0.165f), new Vector3(-0.165f, 0, -0.165f) };
                    hull_tac_front.GetComponent<MeshFilter>().mesh.uv = new Vector2[] {
                            new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, 0) };
                    hull_tac_front.GetComponent<MeshFilter>().mesh.triangles = new int[] { 0, 1, 2, 2, 1, 3 }; ;
                    hull_tac_front.transform.position = active_hull.transform.position;
                    if (leo1a3)
                    {
                        hull_tac_front.transform.localPosition += new Vector3(-0.75f, -1.15f, 1.2f);
                        hull_tac_front.transform.localRotation = Quaternion.Euler(new Vector3(310f, 0f, 180f));
                        hull_tac_front.transform.localScale = new Vector3(0.6f, 0.6f, 0.4f);
                    }
                    else
                    {
                        hull_tac_front.transform.localPosition += new Vector3(-1.45f, 2.4f, 2.25f);
                        hull_tac_front.transform.localRotation = Quaternion.Euler(new Vector3(330f, 180f, 0f));
                        hull_tac_front.transform.localScale = new Vector3(1.3f, 1.1f, 1.1f);
                    }
                    hull_tac_front.GetComponent<MeshFilter>().mesh.RecalculateNormals();

                    GameObject hull_tac_rear = new GameObject("tactical sign rear");
                    hull_tac_rear.transform.parent = active_hull.transform;
                    hull_tac_rear.AddComponent<MeshFilter>();
                    hull_tac_rear.AddComponent<MeshRenderer>();
                    hull_tac_rear.GetComponent<MeshFilter>().mesh = hull_tac_front.GetComponent<MeshFilter>().mesh;
                    hull_tac_rear.GetComponent<MeshRenderer>().material = hull_tac_front.GetComponent<MeshRenderer>().material;
                    hull_tac_rear.transform.position = active_hull.transform.position;
                    if (leo1a3)
                    {
                        hull_tac_rear.transform.localPosition += new Vector3(-0.88f, 5.418f, 1.4f);
                        hull_tac_rear.transform.localRotation = Quaternion.Euler(new Vector3(348f, 0f, 0f));
                        hull_tac_rear.transform.localScale = new Vector3(0.6f, 0.4f, 0.4f);
                    }
                    else
                    {
                        hull_tac_rear.transform.localPosition += new Vector3(-1.78f, 2.8f, -10.85f);
                        hull_tac_rear.transform.localRotation = Quaternion.Euler(new Vector3(-100f, 0f, 0f));
                        hull_tac_rear.transform.localScale = new Vector3(1f, 1f, 0.8f);
                    }

                    GameObject mlc_decal = new GameObject("MLC decal");
                    mlc_decal.transform.parent = active_hull.transform;
                    mlc_decal.AddComponent<MeshFilter>();
                    mlc_decal.AddComponent<MeshRenderer>();
                    mlc_decal.GetComponent<MeshRenderer>().material = numbers.material;
                    mlc_decal.GetComponent<MeshRenderer>().material.mainTexture = mlc;
                    mlc_decal.GetComponent<MeshFilter>().mesh = hull_tac_front.GetComponent<MeshFilter>().mesh;
                    mlc_decal.transform.position = active_hull.transform.position;
                    if (leo1a3)
                    {
                        mlc_decal.transform.localPosition += new Vector3(0.84f, -0.62f, 1.48f);
                        mlc_decal.transform.localRotation = Quaternion.Euler(new Vector3(306f, 0f, 180f));
                        mlc_decal.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                    }
                    else
                    {
                        mlc_decal.transform.localPosition += new Vector3(1.6f, 3f, 1.15f);
                        mlc_decal.transform.localRotation = Quaternion.Euler(new Vector3(330f, 180f, 0f));
                        mlc_decal.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                    }
                    mlc_decal.GetComponent<MeshFilter>().mesh.RecalculateNormals();
                }

                if (!mute_logger.Value) { MelonLogger.Msg("Conversions complete on " + vehicle_go.name); }
            }
            activeScene = false;
            yield break;            
        }
    }    
}
