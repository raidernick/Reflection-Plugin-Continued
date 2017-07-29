using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Text;
using System.Reflection;
/*
Layer 0: Default
Layer 1: TransparentFX
Layer 2: Ignore Raycast
Layer 3: 
Layer 4: Water
Layer 5: UI
Layer 6: 
Layer 7: 
Layer 8: PartsList_Icons
Layer 9: Atmosphere
Layer 10: Scaled Scenery
Layer 11: UI_Culled
Layer 12: UI_Main
Layer 13: UI_Mask
Layer 14: Screens
Layer 15: Local Scenery
Layer 16: kerbals
Layer 17: Editor_UI
Layer 18: SkySphere
Layer 19: Disconnected Parts
Layer 20: Internal Space
Layer 21: Part Triggers
Layer 22: KerbalInstructors
Layer 23: ScaledSpaceSun
Layer 24: MapFX
Layer 25: EzGUI_UI
Layer 26: WheelCollidersIgnore
Layer 27: WheelColliders
Layer 28: TerrainColliders
Layer 29: DragRender
Layer 30: SurfaceFX
*/		
namespace ReflectionPlugin
{
    public class ReflectiveShaderModule : PartModule
    {
        [KSPField(isPersistant = false)]
        public int CubeMapSize = 128;
        [KSPField(isPersistant = false)]
        public float FarClipPlane = float.MaxValue;
        [KSPField(isPersistant = false)]
        public float NearClipPlane = -1f;
        [KSPField(isPersistant = false)]
        public string MeshesToChange = "all";
        [KSPField(isPersistant = false)]
        public bool OneFacePerFrame = true;
        [KSPField(isPersistant = false)]
        public float ParallaxHeight = 0.02f;
        [KSPField(isPersistant = false)]
        public float ReflectionStrength = 1.0f;
        [KSPField(isPersistant = false)]
        public Color ReflectionColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [KSPField(isPersistant = false)]
        public Color _Color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        [KSPField(isPersistant = false)]
        public string ShaderName = "";
        [KSPField(isPersistant = false)]
        public float Shininess = -1f;
        [KSPField(isPersistant = false)]
        public float SpecColorA = 0.5f;
        [KSPField(isPersistant = false)]
        public float SpecColorB = 0.5f;
        [KSPField(isPersistant = false)]
        public float SpecColorG = 0.5f;
        [KSPField(isPersistant = false)]
        public float SpecColorR = -1f;
        [KSPField(isPersistant = false)]
        public float rimFalloff = 0;
        [KSPField(isPersistant = false)]
        public Color rimColor = new Color(0f, 0f, 0f, 0f);
        [KSPField(isPersistant = false)]
        public bool realTimeReflection = true;
        [KSPField(isPersistant = false)]
        public double updateRate = 300.0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Last Scene", guiUnits = "", guiFormat = "G")]
        public string lastScene = "";
        [KSPField(isPersistant = false, guiActive = false, guiName = "Shader", guiUnits = "", guiFormat = "G")]
        public string scriptStatus = "";



        private Shader _rShader;
        private ReflectiveScript reflectiveScript = null;

        public override void OnStart(PartModule.StartState state)
        {
            Debug.Log((object)"RP: Starting ReflectionPlugin .. ");
			GameEvents.onVesselGoOffRails.Add(RefreshReflection);
			GameEvents.onDominantBodyChange.Add(UpdateBody);
            if (ShaderName == string.Empty)
            {
                Debug.Log((object)"RP: Defaulting shader to \"Reflective/VertexLit\"");
                ShaderName = "Reflective/VertexLit";
            }
            _rShader = Shader.Find(ShaderName);
            Shader fallbackShader = Shader.Find("Reflective/VertexLit");

            if ((UnityEngine.Object)_rShader == (UnityEngine.Object)null)
            {
				#if DEBUG
                Debug.LogWarning((object)string.Format("RP: Could not find the specified shader \"{0}\".", (object)ShaderName));
                Debug.LogWarning((object)"RP: Simple reflective shaders:");
                Debug.LogWarning((object)"RP: -\"Reflective/Diffuse\"");
                Debug.LogWarning((object)"RP: -\"Reflective/Specular\"");
                Debug.LogWarning((object)"RP: -\"Reflective/VertexLit\"");
                Debug.LogWarning((object)"RP: Advanced reflective shaders:");
                Debug.LogWarning((object)"RP: -\"Reflective/Bumped Diffuse\"");
                Debug.LogWarning((object)"RP: -\"Reflective/Bumped Specular\"");
                Debug.LogWarning((object)"RP: -\"Reflective/Bumped Unlit\"");
                Debug.LogWarning((object)"RP: -\"Reflective/Bumped VertexLit\"");
                Debug.LogWarning((object)"RP: -\"Reflective/Parallax Diffuse\"");
                Debug.LogWarning((object)"RP: -\"Reflective/Parallax Specular\"");
				#endif

                // Try to handle fallback later during material building
                // This is so we can try to load and deserialize missing shaders from compiled shader code.
                _rShader = null;
                //_rShader = Shader.Find(ShaderName);
                //if ((UnityEngine.Object)_rShader == (UnityEngine.Object)null)
                //{
                //    Debug.LogWarning((object)"RP: Fallback shader VertexLit failed.");
                //}
            }
            else
            {
                Debug.LogWarning((object)string.Format("RP: Found shader \"{0}\".", (object)_rShader.name));
            }

            if (MeshesToChange == string.Empty)
            {
                Debug.Log((object)"RP: Applying changes to part ..");
                ReplaceShader(part.FindModelComponent<UnityEngine.Renderer>());
            }
            else
            {
                Debug.Log((object)"RP: Applying changes to meshes ..");
                List<string> meshNamesList = Enumerable.ToList<string>((IEnumerable<string>)MeshesToChange.Split(new char[1] { ',' }));
                MeshFilter[] modelComponents = part.FindModelComponents<MeshFilter>().ToArray();
                if (MeshesToChange == "all")
                {
                    foreach (MeshFilter meshFilter in modelComponents)
                    {
                        Debug.Log((object)("RP: Applying changes to mesh: " + meshFilter.name));
                        ReplaceShader(meshFilter.GetComponent<UnityEngine.Renderer>());
                    }
                }
                else
                {
                    foreach (MeshFilter meshFilter in Enumerable.Where<MeshFilter>((IEnumerable<MeshFilter>)modelComponents, (Func<MeshFilter, bool>)(mesh => meshNamesList.Contains(mesh.name))))
                    {
                        Debug.Log((object)("RP: Applying changes to mesh: " + meshFilter.name));
                        ReplaceShader(meshFilter.GetComponent<UnityEngine.Renderer>());
                    }
                }
            }
            Debug.Log((object)"RP: Done.");
        }


        private void ReplaceShader(UnityEngine.Renderer pRenderer)
        {
            if ((object)pRenderer != null)
            {
                Debug.Log((object)string.Format("RP: Renderer found: {0}", (object)_rShader));
                Material material;
				string shaderName = ShaderName;
				string resourceNamesString = "EMBEDDED RESOURCE NAMES\n";

				shaderName = shaderName.Replace (" ", "_");
				shaderName = shaderName.Replace ("/", ".");

                if (_rShader == null)
                {
                    Debug.Log((object)("RP: null shader. Trying to retrieve ReflectionPlugin.Shaders." + shaderName));
                    Assembly assembly = Assembly.GetExecutingAssembly();

					string[] resourceNamesList = assembly.GetManifestResourceNames();
					foreach (string resourceName in resourceNamesList)
					{
						resourceNamesString += resourceName + "\n";
					}

					Debug.Log (resourceNamesString);

					try
					{
						Debug.Log ("[ReflectionPlugin] Looking for resource " + shaderName);
                    	
						StreamReader shaderStreamReader = new StreamReader(assembly.GetManifestResourceStream(/*"ReflectionPlugin.Shaders." + */"ReflectionPlugin.Resources." + shaderName + ".shader"));

						Debug.Log ("[ReflectionPlugin] Got " + shaderName);

						material = new Material(shaderStreamReader.ReadToEnd())
						{
							mainTexture = pRenderer.material.mainTexture
						};
					}
					catch (Exception e)
					{
						Debug.Log ("ReflectionPlugin caught exception " + e.ToString() + " (" + e.Message + ")");

						shaderName = "Reflective.VertexLit";
						Debug.Log ("[ReflectionPlugin] Looking for fallback resource " + shaderName);
						
						StreamReader shaderStreamReader = new StreamReader(assembly.GetManifestResourceStream("ReflectionPlugin.Resources." + shaderName + ".shader"));
						
						Debug.Log ("[ReflectionPlugin] Got " + shaderName);
						
						material = new Material(shaderStreamReader.ReadToEnd())
						{
							mainTexture = pRenderer.material.mainTexture
						};
					}
					Debug.Log ("[ReflectionPlugin] Initialized Material: " + material.ToString ());
                }
                else
                {
                    material = new Material(_rShader)
                    {
                        mainTexture = pRenderer.material.mainTexture
                    };
                }
                Texture texture1 = pRenderer.material.GetTexture("_BumpMap");
                Texture texture2 = pRenderer.material.GetTexture("_Emissive");
                if ((object)texture1 != null)
                {
                    Debug.LogWarning((object)"RP: Found bumpmap texture, applying..");
                    material.SetTexture("_BumpMap", texture1);
                    material.SetTextureScale("_BumpMap", pRenderer.material.GetTextureScale("_BumpMap"));
                }
                if ((object)texture2 != null)
                {
                    Debug.LogWarning((object)"RP: Found heightmap texture, applying..");
                    material.SetTexture("_ParallaxMap", texture2);
                    material.SetFloat("_Parallax", (double)ParallaxHeight < 0.00499999988824129 || (double)ParallaxHeight > 0.0799999982118607 ? 0.02f : ParallaxHeight);
                    material.SetTextureScale("_Parallax", pRenderer.material.GetTextureScale("_Parallax"));
                }
                //try
                //{
                print("RP: Set _Shininess");
                material.SetFloat("_Shininess", (double)Shininess < 0.0 ? pRenderer.material.GetFloat("_Shininess") : Shininess);
                print("RP: Set _SpecColor");
                material.SetColor("_SpecColor", (double)SpecColorR < 0.0 ? pRenderer.material.GetColor("_SpecColor") : new Color(SpecColorR, SpecColorG, SpecColorB, SpecColorA));

                // -1 for ReflectionStrength will actually use ReflectionColor, Individual RGB(A???) values can be set in config.
                if (ReflectionStrength < 0f)
                {
                    print("RP: Set _ReflectColor: " + ReflectionColor.ToString());
                    material.SetColor("_ReflectColor", ReflectionColor);
                }
                else
                {
                    print("RP: Set _ReflectColor (ReflectionStrength: " + ReflectionStrength.ToString() + ")");
                    material.SetColor("_ReflectColor", new Color(ReflectionStrength, ReflectionStrength, ReflectionStrength, ReflectionStrength));
                }

                print("RP: Set _Color");
                material.SetColor("_Color", _Color);
                material.SetFloat("_RimFalloff", rimFalloff);
                material.SetColor("_RimColor", rimColor);

                //}
                //catch (Exception e)
                //{
                //    print("RP: ReplaceShader exception: " + e.ToString() + ": " + e.Message);
                //}
                // Add texture scale support
                material.mainTextureScale = pRenderer.material.mainTextureScale;

                pRenderer.material = material;
                ReflectiveScript _reflectiveScript = part.gameObject.AddComponent<ReflectiveScript>();
                _reflectiveScript.MatRenderer = pRenderer;
				_reflectiveScript.CubemapSize = CubeMapSize;
				_reflectiveScript.FarClipPlane = FarClipPlane;
				_reflectiveScript.NearClipPlane = NearClipPlane;
				_reflectiveScript.OneFacePerFrame = OneFacePerFrame;
				_reflectiveScript.realTimeReflection = realTimeReflection;
				_reflectiveScript.updateRate = updateRate;
				_reflectiveScript.dirty = 7;
				reflectiveScript = _reflectiveScript;
                Debug.Log((object)"RP: Material, shader and texture updated.");
            }
            else
                Debug.LogError((object)("RP: Unable to find a Renderer component on the part. Part: " + part.partName));
        }

		public void UpdateBody(GameEvents.FromToAction<CelestialBody,CelestialBody> onDominantBodyChange)
		{
			CelestialBody toBody = onDominantBodyChange.to;
			if (toBody != null)
			{
				ScaledSpaceFader scaledSpaceFader = toBody.scaledBody.GetComponent<ScaledSpaceFader> ();
				if ((object)scaledSpaceFader != null)
					reflectiveScript.scaledFaderEnd = scaledSpaceFader.fadeEnd;
			}
			RefreshReflection(vessel);
		}

		public void RefreshReflection(Vessel v)
		{
			reflectiveScript.dirty = 7;
		}

        public void Update()
        {
            scriptStatus = reflectiveScript.status;
            lastScene = reflectiveScript.lastScene;
        }

		public void FixedUpdate()
		{
			try
			{
				reflectiveScript.transform.position = part.transform.position;
			}
			catch (Exception e)
			{
				// Nah, screw it, just ignore it.
			}
			//reflectiveScript.transform.rotation = part.transform.rotation;
		}

//        public override void OnUpdate()
//        {
//            base.OnUpdate();
//            lastScene = reflectiveScript.lastScene;
//            lastUpdate = reflectiveScript.lastUpdate;
//            scriptStatus = reflectiveScript.status;
//        }
    }
}
