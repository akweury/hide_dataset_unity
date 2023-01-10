using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;


public class VisualObjs : MonoBehaviour
{
    // useful public variables
    public GameObject table; // https://www.cgtrader.com/items/1875799/download-page
    public List<GameObject> sphereModels;
    public List<GameObject> cubeModels;
    public RenderTexture texture;

    public Shader depthShader;
    public Shader normalShader;

    // useful private variables
    private string _rootPath;
    private string _datasetPath;
    private string _rulePath;
    // private string _savePath;
    
    private string _filePrefix;
    private int _frames;
    private int _ruleFileCounter;
    private RuleJson _rules;
    private int _fileCounter;
    private List<GameObject> _objInstances;
    private List<MeshRenderer> _modelRenderers;
    private List<ObjectStruct> _sceneData;
    private float _scaleFactor = 1;
    private DepthCamera _depthCamera;
    private const float TableWidthBase = 1.5F;
    private const float TableLengthBase = 1.5F;
    private const float TableHeightBase = 0.1F;
    private const int FrameLoop = 10;
    private const int MaxNumTry = 50;
    private const float UnifyRadius = 0.5F;
    // private string _expPath;
    private const float MinimumObjDist = (float)0.1;
    private float _tableLength;
    private float _tableWidth;
    private float _tableHeight;
    private FileInfo[] _files;

    private string _sceneType;
    public int single_idx = 1;

    public List<Material> materials;
    // public SceneStruct SceneData;

    public Material environmentMap;
    public Cubemap danceRoomEnvironment;
    public List<Cubemap> environments;
    public GameObject cursor;
    public GameObject tableModel;

    private GameObject tableInst;

    int maxFileCounter;


    // Start is called before the first frame update
    void Start()
    {
        // path control
        _rootPath = Application.dataPath + "/../../spatial_relation_vector/storage/";
        _rulePath = _rootPath + "../rules";

        _sceneType = "train";
        _datasetPath = _rootPath + "dataset/02.learning_rules/";
        // _sceneType = "test";
        // _datasetPath = _rootPath + "dataset/03.scene_manipulation/";
        
        // _savePath = _datasetPath;
        
        
        Camera cam = Instantiate(Camera.main, Camera.main.transform.position, Camera.main.transform.rotation);
        _depthCamera = new DepthCamera(cam, depthShader, normalShader);
        _depthCamera.Cam.targetTexture = texture;
        RenderSettings.ambientLight = Color.gray;

        
        System.IO.Directory.CreateDirectory(_datasetPath);

        
        // get all rule files
        DirectoryInfo d = new DirectoryInfo(_rulePath);
        _files = d.GetFiles("*.json"); // Getting Rule files

        // load a new rule
        _rules = LoadNewRule(_files[_ruleFileCounter].FullName);
        _fileCounter = 0;
        _ruleFileCounter++;
        // _objInstances = GetNewInstances(_rules);
        // UpdateModels(_rules);

        // instantiate the table
        // adjust table size based on camera sensor size
        _scaleFactor = 4F;
        _tableLength = TableLengthBase * _scaleFactor;
        _tableWidth = TableWidthBase * _scaleFactor;
        _tableHeight = TableHeightBase * _scaleFactor;
        
        Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
            UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));

        table.transform.localScale = new Vector3(_tableLength, _tableHeight, _tableWidth);
        environmentMap.SetTexture("_Tex", danceRoomEnvironment);
    }

    // Update is called once per frame
    void Update()
    {
        // render the scene
        if (_frames % FrameLoop == 0)
        {
            RenderNewScene(_rules, sphereModels, cubeModels, _sceneType);
        }

        // save the scene data if it is a new scene
        if (_frames % FrameLoop == FrameLoop - 1)
        {
            _filePrefix = _datasetPath + _ruleFileCounter.ToString("D2") + "." + _fileCounter.ToString("D5");
            DepthCamera.SaveScene(_filePrefix, _objInstances, _depthCamera, _sceneData);
            DestroyObjs(_objInstances);

            _fileCounter++;
        }


        // load a new rule file
        if (RuleFinished(_rules, _fileCounter))
        {
            DestroyObjs(_objInstances);
            if (_ruleFileCounter >= _files.Length)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
            else
            {
                _rules = LoadNewRule(_files[_ruleFileCounter].FullName);
                _fileCounter = 0;
                _ruleFileCounter++;
            }
        }

        // stop rendering when file number exceeded the threshold
        // UpdateModels(_rules);
        _frames++;
    }

    void RenderNewScene(RuleJson rules, List<GameObject> spheres, List<GameObject> cubes, string sceneType)
    {
        int objNum = rules.RandomObjPerScene + rules.RuleObjPerScene;
        _sceneData = new List<ObjectStruct>(new ObjectStruct[objNum]);
        _objInstances = new List<GameObject>(new GameObject[objNum]);

        _sceneData = FillSceneData(rules, _sceneData, spheres, cubes);
        _sceneData = FillObjsPositions(rules, _sceneData, sceneType);

        int objId = 0;
        foreach (var sceneObj in _sceneData)
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

    List<ObjectStruct> FillSceneData(RuleJson rules, List<ObjectStruct> sceneData, List<GameObject> spheres,
        List<GameObject> cubes)
    {
        RuleJson.ObjProp obj;
        int objId = 0;
        // add rule object
        for (int i = 0; i < rules.RuleObjPerScene; i++)
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

            // sceneData[objId].SetProperty(objId, obj.Shape, obj.Material, Rules.strFloMapping[obj.size]);
            sceneData[objId] = new ObjectStruct(objId, obj.Shape, obj.Material, RuleJson.strFloMapping[obj.size]);
            objId++;
        }

        // add random objects
        for (int i = 0; i < rules.RandomObjPerScene; i++)
        {
            float size;
            int sizeId = UnityEngine.Random.Range(0, 2);
            if (sizeId == 0)
            {
                size = RuleJson.strFloMapping["small"];
            }
            else
            {
                size = RuleJson.strFloMapping["big"];
            }

            string shape;
            int shapeId = UnityEngine.Random.Range(0, 2);
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

            sceneData[objId] = new ObjectStruct(objId, shape, material, size);

            // sceneData[objId].SetProperty(objId, shape, material, size);
            objId++;
        }

        return sceneData;
    }

    // void SaveScene(List<GameObject> objInstances, DepthCamera depthCamera, List<ObjectStruct> sceneData)
    // {
    //     DepthCamera.Calibration camera0 = depthCamera.GetCameraMatrix();
    //     // Calibration camera0 = getCameraMatrix();
    //     string filePrefix = _savePath + _ruleFileCounter.ToString("D2") + "." + _fileCounter.ToString("D5");
    //
    //     string depthFileName = filePrefix + ".depth0.png";
    //     DepthCamera.DepthMap depthMap0 = depthCamera.CaptureDepth(depthFileName, objInstances);
    //
    //     string normalFileName = filePrefix + ".normal0.png";
    //     depthCamera.CaptureNormal(normalFileName, camera0.R, objInstances);
    //
    //     string sceneFileName = filePrefix + ".image.png";
    //     depthCamera.CaptureScene(sceneFileName);
    //
    //     string dataFileName = filePrefix + ".data0.json";
    //     depthCamera.writeDataFileOneView(dataFileName, camera0, depthMap0, sceneData);
    //
    //     environmentMap.SetTexture("_Tex", danceRoomEnvironment);
    //     DestroyObjs(objInstances);
    // }

    // void UpdateModels(RuleJson rules)
    // {
    //     if (_fileCounter >= rules.TrainNum + rules.ValNum)
    //     {
    //         _sceneType = "test";
    //         _savePath = _datasetPath + "test/";
    //     }
    //
    //     // generate validation scenes
    //     else if (_fileCounter >= rules.TrainNum)
    //     {
    //         _sceneType = "val";
    //         _savePath = _datasetPath + "val/";
    //     }
    //
    //     // generate training scenes
    //     else if (_fileCounter >= 0)
    //     {
    //         _sceneType = "train";
    //         _savePath = _datasetPath + "train/";
    //     }
    // }


    void DestroyObjs(List<GameObject> objs)
    {
        foreach (var obj in objs)
        {
            if (obj) Destroy(obj);
        }
    }


    bool RuleFinished(RuleJson rules, int fileCounter)
    {
        if (fileCounter >= rules.SceneNum)
        {
            return true;
        }

        return false;
    }


    RuleJson LoadNewRule(string ruleFileName)
    {
        StreamReader streamReader = new StreamReader(ruleFileName);
        string json = streamReader.ReadToEnd();
        RuleJson rulesJson = JsonConvert.DeserializeObject<RuleJson>(json);
        return rulesJson;
    }


    List<ObjectStruct> FillObjsPositions(RuleJson rules, List<ObjectStruct> sceneData, string sceneType)
    {
        bool layoutFinished = false;
        while (!layoutFinished)
        {
            int objIdx = 0;
            Vector3 randomCenter = new Vector3(
                table.transform.position[0] + UnityEngine.Random.Range(-(_tableWidth / 2), (_tableWidth / 2)),
                table.transform.position[1] + _tableHeight * 0.5F,
                table.transform.position[2] + UnityEngine.Random.Range(-(_tableLength / 2), (_tableLength / 2)));

            // add rule objects

            for (int i = 0; i < rules.RuleObjPerScene; i++)
            {
                if (String.Equals(sceneType, "test"))
                {
                    sceneData[objIdx] = GetRandomPos(objIdx, sceneData);
                }
                else
                {
                    sceneData[objIdx] = GetRulePos(objIdx, rules.Objs[i], sceneData[objIdx], randomCenter);
                    if (sceneData[objIdx].Position == Vector3.zero)
                    {
                        break;
                    }
                }

                objIdx++;
            }

            // add random objects
            if (objIdx >= rules.RuleObjPerScene)
            {
                for (int i = 0; i < rules.RandomObjPerScene; i++)
                {
                    sceneData[objIdx] = GetRandomPos(objIdx, sceneData);
                    if (sceneData[objIdx].Position == Vector3.zero)
                    {
                        break;
                    }

                    objIdx++;
                }
            }


            layoutFinished = CheckScene(sceneData);
        }

        return sceneData;
    }

    bool CheckScene(List<ObjectStruct> sceneData)
    {
        foreach (var sceneObj in sceneData)
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

    ObjectStruct GetRulePos(int objId, RuleJson.ObjProp objProp, ObjectStruct obj, Vector3 center)
    {
        float newScale = RuleJson.strFloMapping[objProp.size];
        int num_tries = 0;
        float obj_radius = newScale * UnifyRadius;
        Vector3 obj_pos;
        // find a 3D position for the new object
        while (true)
        {
            // exceed the maximum trying time
            num_tries += 1;
            if (num_tries > MaxNumTry) return obj;

            // choose a proper position
            bool pos_good = true;
            // give a random position for the target object
            if (objProp.x == 0F && objProp.z == 0F)
            {
                obj_pos = new Vector3(center[0], center[1] + obj_radius, center[2]);
            }
            // give a position for the rest rule objects based on target object position and info from the json file
            else
            {
                obj_pos = new Vector3(objProp.x + center[0], objProp.y + center[1] + obj_radius, objProp.z + center[2]);

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

    ObjectStruct GetRandomPos(int objIdx, List<ObjectStruct> sceneData)
    {
        float newScale = sceneData[objIdx].Size;
        int num_tries = 0;
        float objRadius;
        Vector3 objPos = new Vector3();

        // find a 3D position for the new object
        while (true)
        {
            num_tries += 1;
            // exceed the maximum trying time
            if (num_tries > MaxNumTry) return sceneData[objIdx];

            // choose new size and position
            objRadius = newScale * UnifyRadius;

            objPos[0] = table.transform.position[0] + UnityEngine.Random.Range(
                -(float)(_tableWidth / 2) + objRadius, (float)(_tableWidth / 2) - objRadius);
            objPos[1] = table.transform.position[1] + _tableHeight * 0.5F + objRadius;
            objPos[2] = table.transform.position[2] + UnityEngine.Random.Range(
                -(float)(_tableLength / 2) + objRadius, (float)(_tableLength / 2) - objRadius);

            // check for overlapping
            bool distsGood = true;
            for (int i = 0; i < objIdx; i++)
            {
                float dx = objPos[0] - sceneData[i].Position[0];
                float dz = objPos[2] - sceneData[i].Position[2];
                float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                if (dist - objRadius - sceneData[i].Size < MinimumObjDist)
                {
                    return sceneData[objIdx];
                }
            }

            if (distsGood) break;
        }

        // create the new object
        // GameObject objInst = NewObjectInstantiate(obj_radius * 2, obj_pos, new_model, "");

        // for cubes, adjust its radius
        if (sceneData[objIdx].Shape == "cube") objRadius *= (float)Math.Sqrt(2);

        // record the object data
        sceneData[objIdx].Position = objPos;
        sceneData[objIdx].Size = objRadius;


        // normal return
        return sceneData[objIdx];
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