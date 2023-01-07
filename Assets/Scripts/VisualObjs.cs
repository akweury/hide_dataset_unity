using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;
using Pngcs.Unity;
using Random = System.Random;


public partial class VisualObjs : MonoBehaviour
{
    // useful public variables
    public GameObject table; // https://www.cgtrader.com/items/1875799/download-page
    public List<GameObject> sphereModels;
    public List<GameObject> cubeModels;
    public RenderTexture texture;

    // useful private variables
    private int _frames;
    private string _rootPath;
    private string _savePath;
    private int _ruleFileCounter;
    private Rules _rules;
    private int _fileCounter;
    private List<GameObject> _objInstances;
    private List<MeshRenderer> _modelRenderers;
    private SceneStruct _sceneData;

    private DepthCamera _depthCamera;
    // private Camera _cam;
    // unchecked variables

    private static int _frameLoop = 10;
    private int _maxNumTry = 50;
    float TABLE_WIDTH_BASE = 1.5F;
    float TABLE_LENGTH_BASE = 1.5F;
    float TABLE_HEIGHT_BASE = 0.1F;
    float UNIFY_RADIUS = 0.5F;
    float MINIMUM_OBJ_DIST = (float)0.1;
    private float MINIMUM_SCALE_RANGE = 0.15F;
    private float MAXIMUM_SCALE_RANGE = 0.3F;

    public int single_idx = 1;

    public List<Material> materials;
    // public SceneStruct SceneData;

    public Material environmentMap;
    public Cubemap danceRoomEnvironment;
    public List<Cubemap> environments;

    public GameObject cursor;


    public Shader depthShader;
    public Shader normalShader;


    public GameObject tableModel;

    private GameObject tableInst;
    float _tableLength;
    float _tableWidth;
    float _tableHeight;

    int maxFileCounter;


    // int modelIdx = -1;
    float scale_factor = 1;

    // string file_type;


    // private Rules rulesJson;
    private string _sceneType;
    private FileInfo[] _files;


    // Start is called before the first frame update
    void Start()
    {
        Camera cam = Instantiate(Camera.main, Camera.main.transform.position, Camera.main.transform.rotation);
        _depthCamera = new DepthCamera(cam, depthShader, normalShader);
        _depthCamera.Cam.targetTexture = texture;
        RenderSettings.ambientLight = Color.gray;

        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic/train");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic/val");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic/test");

        _rootPath = Application.dataPath + "/../CapturedData/data_synthetic/";


        // adjust table size based on camera sensor size
        scale_factor = 4F;
        _tableLength = TABLE_LENGTH_BASE * scale_factor;
        _tableWidth = TABLE_WIDTH_BASE * scale_factor;
        _tableHeight = TABLE_HEIGHT_BASE * scale_factor;

        // get all rule files
        DirectoryInfo d = new DirectoryInfo(Application.dataPath + "/Scripts/Rules");
        _files = d.GetFiles("*.json"); // Getting Rule files

        // load a new rule
        _rules = LoadNewRule(_files[_ruleFileCounter].FullName);
        _fileCounter = 0;
        _ruleFileCounter++;
        // _objInstances = GetNewInstances(_rules);
        UpdateModels(_rules);

        // instantiate the table
        Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
            UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));

        table.transform.localScale = new Vector3(_tableLength, _tableHeight, _tableWidth);
        environmentMap.SetTexture("_Tex", danceRoomEnvironment);

        // _modelRenderers = new List<MeshRenderer>(new MeshRenderer[_models.Count]);
        // foreach (var model in _models)
        // {
        //     _modelRenderers.Add(model.GetComponent<MeshRenderer>());
        // }
    }

    // Update is called once per frame
    void Update()
    {
        // render the scene
        if (_frames % _frameLoop == 0)
        {
            
            RenderNewScene(_rules, sphereModels, cubeModels, _sceneType);
        }

        // save the scene data if it is a new scene
        if (_frames % _frameLoop == _frameLoop - 1)
        {
            SaveScene(_objInstances, _depthCamera, _sceneData);
            _fileCounter++;
        }

        // load a new rule file
        if (RuleFinished(_rules, _fileCounter))
        {
            DestroyObjs(_objInstances);
            _rules = LoadNewRule(_files[_ruleFileCounter].FullName);
            _fileCounter = 0;
            _ruleFileCounter++;
            // _objInstances = GetNewInstances(_rules);
            UpdateModels(_rules);
        }

        // stop rendering when file number exceeded the threshold
        if (CheckFinished())
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }

        _frames++;
    }

    void RenderNewScene(Rules rules, List<GameObject> spheres, List<GameObject> cubes, string sceneType)
    {
        int objNum = rules.RandomObjPerScene + rules.RuleObjPerScene;
        _sceneData = new SceneStruct(objNum);
        _objInstances = new List<GameObject>(new GameObject[objNum]);
        
        _sceneData = FillSceneData(rules, _sceneData, spheres, cubes);
        _sceneData = FillObjsPositions(rules, _sceneData, sceneType);

        int objId = 0;
        foreach (var sceneObj in _sceneData.Objects)
        {
            float objSize = sceneObj.Size;

            // for cubes, adjust its radius
            if (sceneObj.Shape == "cube") objSize = sceneObj.Size / (float)Math.Sqrt(2);

            GameObject objModel = spheres[0];
            if (sceneObj.Shape == "sphere")
            {
                foreach (GameObject sphere in spheres)
                {
                    if (sphere.name == sceneObj.Material)
                    {
                        objModel = sphere;
                        break;
                    }
                }
            }
            else if (sceneObj.Shape == "cube")
            {
                foreach (GameObject cube in cubes)
                {
                    if (cube.name == sceneObj.Material)
                    {
                        objModel = cube;
                        break;
                    }
                }
            }
            _objInstances[objId] = NewObjectInstantiate(objSize * 2, sceneObj.Position, objModel);
            objId++;
        }
    }

    // ObjProp RandomMaterial(ObjProp obj, List<GameObject> spheres, List<GameObject> cubes)
    // {
    //     if (obj.Material == "")
    //     {
    //         if (obj.Shape == "cube")
    //         {
    //             int cubeId = UnityEngine.Random.Range(0, cubes.Count);
    //             obj.Material = cubes[cubeId].name;
    //         }
    //         else if (obj.Shape == "sphere")
    //         {
    //             int sphereId = UnityEngine.Random.Range(0, spheres.Count);
    //             obj.Material = spheres[sphereId].name;
    //         }
    //     }
    //     return obj;
    // }

    SceneStruct FillSceneData(Rules rules, SceneStruct sceneData, List<GameObject> spheres, List<GameObject> cubes)
    {
        ObjProp obj;
        int objId = 0;
        // add rule object
        for (int i = 0; i < sceneData.Objects.Count; i++)
        {
            obj = rules.Objs[i];
            if (obj.Material == "")
            {
                if (obj.Shape == "cube")
                {
                    int cubeId = UnityEngine.Random.Range(0, cubes.Count);
                    obj.Material = cubes[cubeId].name;
                }
                else if (obj.Shape == "sphere")
                {
                    int sphereId = UnityEngine.Random.Range(0, spheres.Count);
                    obj.Material = spheres[sphereId].name;
                }
            }
            sceneData.Objects[objId] = new ObjectStruct(objId, obj.Shape, obj.Material);
            objId++;
        }
        // add random objects
        for (int i = 0; i < rules.RandomObjPerScene; i++)
        {
            int shapeId = UnityEngine.Random.Range(0, 2);
            string shape;
            string material;
            if (shapeId == 0)
            {
                shape = "cube";    
                int cubeId = UnityEngine.Random.Range(0, cubes.Count);
                material = cubes[cubeId].name;
            }
            else
            {
                shape = "sphere";    
                int sphereId = UnityEngine.Random.Range(0, spheres.Count);
                material = spheres[sphereId].name;
            }
            sceneData.Objects[objId] = new ObjectStruct(objId, shape, material);
            objId++;
        }
        return sceneData;
    }

    void SaveScene(List<GameObject> objInstances, DepthCamera depthCamera, SceneStruct sceneData)
    {
        DepthCamera.Calibration camera0 = depthCamera.GetCameraMatrix();
        // Calibration camera0 = getCameraMatrix();
        string filePrefix = _savePath + _ruleFileCounter.ToString("D2") + "." + _fileCounter.ToString("D5");

        string depthFileName = filePrefix + ".depth0.png";
        DepthCamera.DepthMap depthMap0 = depthCamera.CaptureDepth(depthFileName, objInstances);

        string normalFileName = filePrefix + ".normal0.png";
        depthCamera.CaptureNormal(normalFileName, camera0.R, objInstances);

        string sceneFileName = filePrefix + ".image.png";
        depthCamera.CaptureScene(sceneFileName);

        string dataFileName = filePrefix + ".data0.json";
        depthCamera.writeDataFileOneView(dataFileName, camera0, depthMap0, sceneData);

        environmentMap.SetTexture("_Tex", danceRoomEnvironment);
        DestroyObjs(objInstances);
    }

    void UpdateModels(Rules rules)
    {
        if (_fileCounter >= rules.TrainNum + rules.ValNum)
        {
            _sceneType = "test";
            _savePath = _rootPath + "test/";
        }

        // generate validation scenes
        if (_fileCounter >= rules.TrainNum)
        {
            _sceneType = "val";
            _savePath = _rootPath + "val/";
        }

        // generate training scenes
        if (_fileCounter >= 0)
        {
            _sceneType = "train";
            _savePath = _rootPath + "train/";
        }
    }


    void DestroyObjs(List<GameObject> objs)
    {
        foreach (var obj in objs)
        {
            if (obj) Destroy(obj);
        }
    }


    bool RuleFinished(Rules rules, int fileCounter)
    {
        if (fileCounter >= rules.TrainNum + rules.TestNum + rules.ValNum)
        {
            return true;
        }

        return false;
    }


    Rules LoadNewRule(string ruleFileName)
    {
        StreamReader streamReader = new StreamReader(ruleFileName);
        string json = streamReader.ReadToEnd();
        Rules rulesJson = JsonConvert.DeserializeObject<Rules>(json);
        return rulesJson;
    }

    bool CheckFinished()
    {
        if (_ruleFileCounter >= _files.Length)
        {
            return true;
        }

        return false;
    }


    SceneStruct FillObjsPositions(Rules rules, SceneStruct sceneData, string sceneType)
    {
        bool layoutFinished = false;
        while (!layoutFinished)
        {
            int objIdx = 0;
            
            // add rule objects
            for (int i = 0; i < sceneData.Objects.Count; i++)
            {
                if (String.Equals(sceneType, "test"))
                {
                    sceneData.Objects[objIdx] = GetRandomPos(objIdx, sceneData);
                }
                else
                {
                    sceneData.Objects[objIdx] = GetRulePos(objIdx, rules.Objs[i], sceneData.Objects[objIdx]);                    
                }
                objIdx++;
            }
            // add random objects
            for (int i = 0; i < rules.RandomObjPerScene; i++)
            {
                sceneData.Objects[objIdx] = GetRandomPos(objIdx, sceneData);
                objIdx++;
            }

            layoutFinished = CheckScene(sceneData);
        }

        return sceneData;
    }

    bool CheckScene(SceneStruct sceneData)
    {
        foreach (var sceneObj in sceneData.Objects)
        {
            if (sceneObj == null)
            {
                return false;
            }
            if (sceneObj.Position == Vector3.zero)
            {
                return false;
            }
        }

        return true;
    }

    ObjectStruct GetRulePos(int objId, ObjProp objProp, ObjectStruct obj)
    {
        float newScale = strFloMapping[objProp.size];
        int num_tries = 0;
        float obj_radius = newScale * UNIFY_RADIUS;
        Vector3 obj_pos;
        // find a 3D position for the new object
        while (true)
        {
            // exceed the maximum trying time
            num_tries += 1;
            if (num_tries > _maxNumTry) return obj;

            // choose a proper position
            bool pos_good = true;
            // give a random position for the target object
            if (objProp.x == 0F && objProp.z == 0F)
            {
                obj_pos = new Vector3(
                    table.transform.position[0] +
                    UnityEngine.Random.Range(-(_tableWidth / 2) + obj_radius, (_tableWidth / 2) - obj_radius),
                    table.transform.position[1] + _tableHeight * 0.5F + obj_radius,
                    table.transform.position[2] +
                    UnityEngine.Random.Range(-(_tableLength / 2) + obj_radius, (_tableLength / 2) - obj_radius));
            }
            // give a position for the rest rule objects based on target object position and info from the json file
            else
            {
                obj_pos = new Vector3(
                    objProp.x + obj.Position[0],
                    table.transform.position[1] + _tableHeight * 0.5F + obj_radius,
                    objProp.z + obj.Position[2]);

                // check if new position locates in the area of table
                if (obj_pos[0] < -(float)(_tableWidth / 2) + obj_radius ||
                    obj_pos[0] > (float)(_tableWidth / 2) - obj_radius ||
                    obj_pos[2] < -(float)(_tableLength / 2) + obj_radius ||
                    obj_pos[2] > (float)(_tableLength / 2) - obj_radius
                   )
                {
                    pos_good = false;
                    return obj;
                }
            }

            if (pos_good) break;
        }

        // for cubes, adjust its radius
        if (objProp.Shape == "cube") obj_radius *= (float)Math.Sqrt(2);

        // record the object data

        obj.Size = obj_radius;
        obj.Position = obj_pos;
        return obj;
    }

    // SceneStruct RandomPos(float newScale, string newShape, string newMaterial, int objIdx, SceneStruct sceneData)
    // {
    //     int numTries = 0;
    //     Vector3 objPos = new Vector3();
    //     float objRadius = newScale * UNIFY_RADIUS;
    //     while (true)
    //     {
    //         // check if exceed the maximum trying time
    //         numTries += 1;
    //         if (numTries > _maxNumTry) return sceneData;
    //
    //         // choose new size and position
    //
    //
    //         objPos[0] = table.transform.position[0] + UnityEngine.Random.Range(
    //             -(float)(_tableWidth / 2) + objRadius, (float)(_tableWidth / 2) - objRadius);
    //         objPos[1] = table.transform.position[1] + _tableHeight * 0.5F + objRadius;
    //         objPos[2] = table.transform.position[2] + UnityEngine.Random.Range(
    //             -(float)(_tableLength / 2) + objRadius, (float)(_tableLength / 2) - objRadius);
    //
    //         // check for overlapping
    //         bool distGood = true;
    //         // bool margins_good = true;
    //         for (int i = 0; i < objIdx; i++)
    //         {
    //             float dx = objPos[0] - sceneData.Objects[i].Position[0];
    //             float dz = objPos[2] - sceneData.Objects[i].Position[2];
    //             float dist = (float)Math.Sqrt(dx * dx + dz * dz);
    //             if (dist - objRadius - sceneData.Objects[i].Size < MINIMUM_OBJ_DIST)
    //             {
    //                 // distGood = false;
    //                 return sceneData;
    //             }
    //         }
    //
    //         break;
    //     }
    //
    //     // for cubes, adjust its radius
    //     if (newShape == "cube") objRadius *= (float)Math.Sqrt(2);
    //
    //     // record the object data
    //     sceneData.Objects[objIdx] = new ObjectStruct(objIdx, newShape, newMaterial, objRadius, objPos);
    //
    //     return sceneData;
    // }

    ObjectStruct GetRandomPos(int objIdx, SceneStruct sceneData)
    {
        int num_tries = 0;
        float new_scale;
        float objRadius;
        Vector3 objPos = new Vector3();

        // find a 3D position for the new object
        while (true)
        {
            num_tries += 1;
            // exceed the maximum trying time
            if (num_tries > _maxNumTry) return sceneData.Objects[objIdx];

            // choose new size and position
            new_scale = UnityEngine.Random.Range(MINIMUM_SCALE_RANGE, MAXIMUM_SCALE_RANGE) * scale_factor;
            objRadius = new_scale * UNIFY_RADIUS;

            objPos[0] = table.transform.position[0] + UnityEngine.Random.Range(
                -(float)(_tableWidth / 2) + objRadius, (float)(_tableWidth / 2) - objRadius);
            objPos[1] = table.transform.position[1] + _tableHeight * 0.5F + objRadius;
            objPos[2] = table.transform.position[2] + UnityEngine.Random.Range(
                -(float)(_tableLength / 2) + objRadius, (float)(_tableLength / 2) - objRadius);

            // check for overlapping
            bool dists_good = true;
            // bool margins_good = true;
            for (int i = 0; i < objIdx; i++)
            {
                float dx = objPos[0] - sceneData.Objects[i].Position[0];
                float dz = objPos[2] - sceneData.Objects[i].Position[2];
                float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                if (dist - objRadius - sceneData.Objects[i].Size < MINIMUM_OBJ_DIST)
                {
                    dists_good = false;
                    return sceneData.Objects[i];
                }
            }

            if (dists_good) break;
        }

        // create the new object
        // GameObject objInst = NewObjectInstantiate(obj_radius * 2, obj_pos, new_model, "");

        // for cubes, adjust its radius
        if (sceneData.Objects[objIdx].Shape == "cube") objRadius *= (float)Math.Sqrt(2);

        // record the object data
        sceneData.Objects[objIdx].Position = objPos;
        sceneData.Objects[objIdx].Size = objRadius;
        
        return sceneData.Objects[objIdx];
    }

    GameObject NewObjectInstantiate(float scale, Vector3 newPoint, GameObject newModel)
    {
        // place the object on the table
        Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
            UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));
        Vector3 position = new Vector3(newPoint.x, newPoint.y, newPoint.z);
        GameObject objInst = Instantiate(newModel, position, rotation);

        // if (material != "")
        // {
        //     newModelRenderer.material = materials[(int)strFloMapping[material]];
        // }
        // else
        // {
        //     newModelRenderer.material = materials[UnityEngine.Random.Range(0, materials.Count - 1)];
        // }

        objInst.name = newModel.name;
        objInst.transform.localScale = new Vector3(scale, scale, scale);

        return objInst;
    }
}