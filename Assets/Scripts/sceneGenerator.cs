// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
//
// public class SceneGenerator : MonoBehaviour
// {
//     
//     // useful public variables
//     public GameObject table; // https://www.cgtrader.com/items/1875799/download-page
//     public List<GameObject> sphereModels;
//     public List<GameObject> cubeModels;
//     public RenderTexture texture;
//
//     public Shader depthShader;
//     public Shader normalShader;
//     
//     // useful private variables
//     private int _frames;
//     private string _rootPath;
//     private string _savePath;
//     private int _ruleFileCounter;
//     private Rules _rules;
//     private int _fileCounter;
//     private List<GameObject> _objInstances;
//     private List<MeshRenderer> _modelRenderers;
//     private List<VisualObjs.ObjectStruct> _sceneData;
//
//     private DepthCamera _depthCamera;
//     
//     
//     // Start is called before the first frame update
//     void Start()
//     {
//         Camera cam = Instantiate(Camera.main, Camera.main.transform.position, Camera.main.transform.rotation);
//         _depthCamera = new DepthCamera(cam, depthShader, normalShader);
//         _depthCamera.Cam.targetTexture = texture;
//         RenderSettings.ambientLight = Color.gray;
//
//         System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic");
//         System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic/train");
//         System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic/val");
//         System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic/test");
//
//         _rootPath = Application.dataPath + "/../CapturedData/data_synthetic/";
//
//
//         // adjust table size based on camera sensor size
//         scale_factor = 4F;
//         _tableLength = TABLE_LENGTH_BASE * scale_factor;
//         _tableWidth = TABLE_WIDTH_BASE * scale_factor;
//         _tableHeight = TABLE_HEIGHT_BASE * scale_factor;
//
//         // get all rule files
//         DirectoryInfo d = new DirectoryInfo(Application.dataPath + "/Scripts/Rules");
//         _files = d.GetFiles("*.json"); // Getting Rule files
//
//         // load a new rule
//         _rules = LoadNewRule(_files[_ruleFileCounter].FullName);
//         _fileCounter = 0;
//         _ruleFileCounter++;
//         // _objInstances = GetNewInstances(_rules);
//         UpdateModels(_rules);
//
//         // instantiate the table
//         Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
//             UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));
//
//         table.transform.localScale = new Vector3(_tableLength, _tableHeight, _tableWidth);
//         environmentMap.SetTexture("_Tex", danceRoomEnvironment);
//
//
//         
//     }
//
//     // Update is called once per frame
//     void Update()
//     {
//         
//     }
// }
