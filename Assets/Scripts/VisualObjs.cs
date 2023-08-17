using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;
using DirectoryInfo = System.IO.DirectoryInfo;
using Random = System.Random;


public class VisualObjs : MonoBehaviour
{
    // useful public variables
    public GameObject table; // https://www.cgtrader.com/items/1875799/download-page
    public List<GameObject> sphereModels;

    public List<GameObject> cubeModels;

    // public List<GameObject> coneModels;
    public List<GameObject> cylinderModels;
    public RenderTexture texture;

    public Shader depthShader;
    public Shader normalShader;

    // useful private variables
    private string _assetsPath;
    private string _dataGroupPath;
    private string _datasetPath;

    private string _rootDatasetPath;
    // private string _subDatasetName;

    private string _rulePath;

    private string _groupPath;
    // private string _savePath;

    private string _filePrefix;
    private int _frames;
    private int _ruleFileCounter;
    private RuleJson[] _rules;
    private int _fileCounter;
    private List<GameObject> _objInstances;
    private List<MeshRenderer> _modelRenderers;
    private List<ObjectStruct> _sceneData;

    private DepthCamera _depthCamera;
    private const float TableWidthBase = 1.5F;
    private const float TableLengthBase = 1.5F;
    private const float TableHeightBase = 0.1F;
    private const int FrameLoop = 10;
    private const int MaxNumTry = 50;
    private const float UnifyRadius = 0.6F;

    private const float MinimumObjDist = (float)0.1;
    private float _tableLength;
    private float _tableWidth;

    private float _tableHeight;
    // private FileInfo[] _negRuleFiles;
    // private FileInfo[] _posRuleFiles;

    public string _useType;
    private string _sceneSign;
    public int sceneNum;
    public String expGroup;
    public String groupSize;
    public String expName;
    public float tableScale;
    public float objScale;
    public float posScale;

    private GameObject tableInst;

    // private string[] _sceneType = { "train", "EOF" };
    // private string[] _sceneType = { "val", "EOF" };
    private string[] _sceneType = { "test", "EOF" };

    private int _sceneTypeCounter = 0;
    int maxFileCounter;
    
    // Start is called before the first frame update
    void Start()
    {
        // path control
        _assetsPath = Application.dataPath + "/";
        // _useType = "test";
        _sceneTypeCounter += 1;

        _rootDatasetPath = _assetsPath + "../../storage/" + expGroup + "/";

        _groupPath = _assetsPath + "Scripts/Rules/" + groupSize + "/";
        _rulePath = _groupPath + expName + "/";
        _datasetPath = _rootDatasetPath + expName + "/";

        Camera cam = Instantiate(Camera.main, Camera.main.transform.position, Camera.main.transform.rotation);
        _depthCamera = new DepthCamera(cam, depthShader, normalShader);
        _depthCamera.Cam.targetTexture = texture;
        RenderSettings.ambientLight = Color.gray;

        System.IO.Directory.CreateDirectory(_rootDatasetPath);
        System.IO.Directory.CreateDirectory(_datasetPath);
        System.IO.Directory.CreateDirectory(_datasetPath + "test");
        System.IO.Directory.CreateDirectory(_datasetPath + "train");
        System.IO.Directory.CreateDirectory(_datasetPath + "val");
        System.IO.Directory.CreateDirectory(_datasetPath + "train" + "/" + "false");
        System.IO.Directory.CreateDirectory(_datasetPath + "test" + "/" + "false");
        System.IO.Directory.CreateDirectory(_datasetPath + "val" + "/" + "false");
        System.IO.Directory.CreateDirectory(_datasetPath + "train" + "/" + "true");
        System.IO.Directory.CreateDirectory(_datasetPath + "test" + "/" + "true");
        System.IO.Directory.CreateDirectory(_datasetPath + "val" + "/" + "true");


        // get positive rule files
        String positiveRulePath = _groupPath + expName;
        DirectoryInfo posD = new DirectoryInfo(positiveRulePath);
        FileInfo[] posRuleFiles = posD.GetFiles("*.json"); // Getting Rule files
        RuleJson[] posRules = new RuleJson[posRuleFiles.Length];
        for (int i = 0; i < posRuleFiles.Length; i++)
        {
            posRules[i] = LoadNewRule(posRuleFiles[i].FullName);
            posRules[i].SceneType = "positive";
            posRules[i].SceneNum = sceneNum / posRuleFiles.Length;
        }

        // get negative rule files
        DirectoryInfo groupD = new DirectoryInfo(_groupPath);
        FileInfo[] randomRules = groupD.GetFiles("*.json"); // Getting Rule files
        var groupLatterPaths = Directory.GetDirectories(_groupPath);
        FileInfo[] negRuleFiles = new FileInfo[groupLatterPaths.Length - 1];
        int negRuleCounter = 0;
        for (int i = 0; i < groupLatterPaths.Length; i++)
        {
            if (groupLatterPaths[i] == positiveRulePath)
            {
                continue;
            }

            DirectoryInfo negD = new DirectoryInfo(groupLatterPaths[i]);
            negRuleFiles[negRuleCounter] = negD.GetFiles("*.json")[0]; // Getting Rule files
            negRuleCounter += 1;
        }

        RuleJson[] negRules = new RuleJson[negRuleFiles.Length + 1];
        for (int i = 0; i < negRuleFiles.Length; i++)
        {
            negRules[i] = LoadNewRule(negRuleFiles[i].FullName);
            negRules[i].SceneType = "negative";
            negRules[i].SceneNum = sceneNum / (negRuleFiles.Length + 1);
        }

        negRules[negRuleFiles.Length] = LoadNewRule(randomRules[0].FullName);
        negRules[negRuleFiles.Length].SceneType = "negative";
        negRules[negRuleFiles.Length].SceneNum = sceneNum / (negRuleFiles.Length + 1);


        // concatenate all negative rules to one single array
        _rules = new RuleJson[posRules.Length + negRules.Length];
        posRules.CopyTo(_rules, 0);
        negRules.CopyTo(_rules, posRules.Length);

        _fileCounter = 0;
        _ruleFileCounter = 0;
        // _objInstances = GetNewInstances(_rules);
        // UpdateModels(_rules);

        // instantiate the table
        // adjust table size based on camera sensor size

        _tableLength = TableLengthBase * tableScale;
        _tableWidth = TableWidthBase * tableScale;
        _tableHeight = TableHeightBase * tableScale;

        Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
            UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));

        table.transform.localScale = new Vector3(_tableLength, _tableHeight, _tableWidth);
        // environmentMap.SetTexture("_Tex", danceRoomEnvironment);
    }

    // Update is called once per frame
    void Update()
    {
        // render the scene
        if (_frames % FrameLoop == 0)
        {
            RenderNewScene(_rules[_ruleFileCounter], sphereModels, cubeModels, cylinderModels);
        }

        // save the scene data if it is a new scene
        if (_frames % FrameLoop == FrameLoop - 1)
        {
            if (_rules[_ruleFileCounter].SceneType == "positive")
            {
                _filePrefix = _datasetPath + _useType + "/true/" + _ruleFileCounter.ToString("D2") + "." +
                              _fileCounter.ToString("D5");
            }
            else
            {
                _filePrefix = _datasetPath + _useType + "/false/" + _ruleFileCounter.ToString("D2") + "." +
                              _fileCounter.ToString("D5");
            }

            DepthCamera.SaveScene(_filePrefix, _objInstances, _depthCamera, _sceneData);
            DestroyObjs(_objInstances);

            _fileCounter++;
        }


        // load a new rule file
        if (RuleFinished(_rules[_ruleFileCounter], _fileCounter))
        {
            DestroyObjs(_objInstances);

            // exit the program
            if (_ruleFileCounter >= _rules.Length - 1)
            {
                _useType = _sceneType[_sceneTypeCounter];
                _sceneTypeCounter += 1;
                // update the scene type and reset parameters
                _fileCounter = 0;
                _ruleFileCounter = 0;

                if (_useType == "EOF")
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                }
            }
            else
            {
                // set parameters for loading new rules
                _fileCounter = 0;
                _ruleFileCounter++;
            }
        }

        // stop rendering when file number exceeded the threshold
        // UpdateModels(_rules);
        _frames++;
    }

    void RenderNewScene(RuleJson rules, List<GameObject> spheres, List<GameObject> cubes, List<GameObject> cylinders)
    {
        // var objNum = rules.RandomObjPerScene + rules.RuleObjPerScene;
        // _sceneData = new List<ObjectStruct>(new ObjectStruct[objNum]);
        // _objInstances = new List<GameObject>(new GameObject[objNum]);

        if (expGroup == "custom_scenes")
        {
            // var objNum = rules.RandomObjPerScene + rules.RuleObjPerScene;
            // _sceneData = new List<ObjectStruct>(new ObjectStruct[objNum]);
            
            
            var position = table.transform.position;
            var centerPoint = new Vector3(position[0], position[1] + _tableHeight * 0.5F, position[2]);
            var customScenes = new CustomScenes(objScale, centerPoint, _tableWidth, UnifyRadius);

            _sceneData = expName switch
            {
                "diagonal" => customScenes.DiagScene(rules.ShapeType),
                "diagonal_high_res" => customScenes.DiagScene(rules.ShapeType),
                "close" => customScenes.CloseScene(rules.ShapeType),
                "red_cube_and_random_sphere" => customScenes.ExistScene(rules.ShapeType),
                "square" => customScenes.SquareScene(rules.ShapeType),
                _ => customScenes.SquareScene(rules.ShapeType),
            };
            _objInstances = new List<GameObject>(new GameObject[customScenes.objTotalNum]);
        }
        else
        {
            var objNum = rules.RandomObjPerScene + rules.RuleObjPerScene;
            _sceneData = new List<ObjectStruct>(new ObjectStruct[objNum]);
            _objInstances = new List<GameObject>(new GameObject[objNum]);
            
            _sceneData = FillSceneData(rules, _sceneData, spheres, cubes, cylinders);
            _sceneData = FillObjsPositions(rules, _sceneData);
        }


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
                    if (sphere.name.Contains(sceneObj.Material))
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
                    if (cube.name.Contains(sceneObj.Material))
                    {
                        objModel = cube;
                        break;
                    }
                }
            }
            // else if (sceneObj.Shape == "cone")
            // {
            //     foreach (GameObject cone in cones)
            //     {
            //         if (cone.name.Contains(sceneObj.Material))
            //         {
            //             objModel = cone;
            //             break;
            //         }
            //     }
            // }
            else if (sceneObj.Shape == "cylinder")
            {
                foreach (GameObject cylinder in cylinders)
                {
                    if (cylinder.name.Contains(sceneObj.Material))
                    {
                        objModel = cylinder;
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
        List<GameObject> cubes, List<GameObject> cylinders)
    {
        RuleJson.ObjProp obj;
        int objId = 0;
        string[] objShapes = new string[rules.RuleObjPerScene];

        Random rnd = new Random();
        int[] totalColorID = new int[] { 0, 1, 2 };
        int[] randomColorID = totalColorID.OrderBy(x => rnd.Next()).ToArray();
        string[] objColors = new string[rules.RuleObjPerScene];
        int[] colorIDs = new int[rules.RuleObjPerScene];
        IEnumerable<int> uniqueColorItems;
        do
        {
            for (int i = 0; i < rules.RuleObjPerScene; i++)
            {
                int randomColorIndex = UnityEngine.Random.Range(0, rules.MaxColorVariation);
                colorIDs[i] = randomColorID[randomColorIndex];
            }

            uniqueColorItems = colorIDs.Distinct<int>();
        } while (uniqueColorItems.Count() < rules.MinColorVariation);


        if (rules.ShapeType == "random")
        {
            int[] totalShapeID = new int[] { 0, 1, 2 };
            int[] randomShapeID = totalShapeID.OrderBy(x => rnd.Next()).ToArray();
            int[] shapeIDs = new int[rules.RuleObjPerScene];
            IEnumerable<int> uniqueShapeItems;
            do
            {
                for (int i = 0; i < rules.RuleObjPerScene; i++)
                {
                    int randomShapeIndex = UnityEngine.Random.Range(0, rules.MaxShapeVariation);
                    shapeIDs[i] = randomShapeID[randomShapeIndex];
                }

                uniqueShapeItems = shapeIDs.Distinct<int>();
            } while (uniqueShapeItems.Count() < rules.MinShapeVariation);

            for (int i = 0; i < rules.RuleObjPerScene; i++)
            {
                int randomShapeIndex = UnityEngine.Random.Range(0, rules.MaxShapeVariation);
                int shapeID = randomShapeID[randomShapeIndex];
                if (shapeID == 0)
                {
                    objShapes[i] = "cube";
                }
                else if (shapeID == 1)
                {
                    objShapes[i] = "sphere";
                }
                // else if (shapeID == 2)
                // {
                //     objShapes[i] = "cone";
                // }
                else if (shapeID == 2)
                {
                    objShapes[i] = "cylinder";
                }
            }
        }
        else
        {
            for (int i = 0; i < rules.RuleObjPerScene; i++)
            {
                objShapes[i] = rules.ShapeType;
            }
        }


        for (int i = 0; i < rules.RuleObjPerScene; i++)
        {
            int randomColorIndex = UnityEngine.Random.Range(0, rules.MaxColorVariation);
            int colorID = randomColorID[randomColorIndex];
            if (colorID == 0)
            {
                objColors[i] = "red";
            }
            else if (colorID == 1)
            {
                objColors[i] = "green";
            }
            else if (colorID == 2)
            {
                objColors[i] = "blue";
            }
        }

        // add rule object
        for (int i = 0; i < rules.RuleObjPerScene; i++)
        {
            obj = rules.Objs[i];
            string objShape = objShapes[i];
            string objColor = objColors[i];
            sceneData[objId] = new ObjectStruct(objId, objShape, objColor, objScale);
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
            else if (shapeId == 1)
            {
                shape = "sphere";
                int sphereId = UnityEngine.Random.Range(0, spheres.Count);
                material = spheres[sphereId].name;
            }
            // else if (shapeId == 2)
            // {
            //     shape = "cone";
            //     int coneId = UnityEngine.Random.Range(0, cones.Count);
            //     material = cones[coneId].name;
            // }
            else
            {
                shape = "cylinder";
                int cylinderId = UnityEngine.Random.Range(0, cylinders.Count);
                material = cylinders[cylinderId].name;
            }

            sceneData[objId] = new ObjectStruct(objId, shape, material, size);

            // sceneData[objId].SetProperty(objId, shape, material, size);
            objId++;
        }

        return sceneData;
    }

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


    List<ObjectStruct> FillObjsPositions(RuleJson rules, List<ObjectStruct> sceneData)
    {
        bool layoutFinished = false;
        while (!layoutFinished)
        {
            int objIdx = 0;
            Vector3 randomCenter = new Vector3(
                table.transform.position[0] /*+UnityEngine.Random.Range(-(_tableWidth / 5), (_tableWidth / 5)) */,
                table.transform.position[1] + _tableHeight * 0.5F,
                table.transform.position[2] /* + UnityEngine.Random.Range(-(_tableLength / 5), (_tableLength / 5)) */);

            // add rule objects
            for (int i = 0; i < rules.RuleObjPerScene; i++)
            {
                if (rules.IsRandomPosition)
                {
                    sceneData[objIdx] = GetRandomPos(objIdx, rules.Objs[i], sceneData);
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
                    sceneData[objIdx] = GetRandomPos(objIdx, rules.Objs[i], sceneData);
                    if (sceneData[objIdx].Position == Vector3.zero)
                    {
                        break;
                    }

                    objIdx++;
                }
            }

            // rotate a random degree
            // var rotateRadians = UnityEngine.Random.Range(-0.17f, 0.17f); // 0.17 radians == 10 degrees
            // var alpha = randomCenter.x;
            // var beta = randomCenter.z;
            // foreach (var obj in sceneData)
            // {
            //     obj.Position.x = alpha + Mathf.Cos(rotateRadians) * (obj.Position.x - alpha) -
            //                      Mathf.Sin(rotateRadians) * (obj.Position.z - beta);
            //     obj.Position.z = beta + Mathf.Sin(rotateRadians) * (obj.Position.x - alpha) +
            //                      Mathf.Cos(rotateRadians) * (obj.Position.z - beta);
            // }


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
        int num_tries = 0;

        float obj_radius = objScale * UnifyRadius;
        Vector3 obj_pos;
        // find a 3D position for the new object
        while (true)
        {
            // exceed the maximum trying time
            num_tries += 1;
            if (num_tries > MaxNumTry) throw new IndexOutOfRangeException();

            // choose a proper position
            bool pos_good = true;
            // give a random position for the target object
            if (objProp.X == 0F && objProp.Z == 0F)
            {
                obj_pos = new Vector3(center[0], center[1] + obj_radius, center[2]);
            }
            // give a position for the rest rule objects based on target object position and info from the json file
            else
            {
                obj_pos = new Vector3(objProp.X * posScale + center[0], objProp.Y + center[1] + obj_radius,
                    objProp.Z * posScale + center[2]);

                // check if new position locates in the area of table
                if (obj_pos[0] < -(_tableWidth / 2) + obj_radius ||
                    // obj_pos[0] > (_tableWidth / 2) - obj_radius ||
                    obj_pos[2] < -(_tableLength / 2) + obj_radius ||
                    obj_pos[2] > (_tableLength / 2) - obj_radius
                   )
                {
                    pos_good = false;
                    return obj;
                }
            }

            if (pos_good) break;
        }

        // for cubes, adjust its radius
        // if (objProp.Shape == "cube") obj_radius *= (float)Math.Sqrt(2);

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

    ObjectStruct GetRandomPos(int objIdx, RuleJson.ObjProp objProp, List<ObjectStruct> sceneData)
    {
        // float newScale = sceneData[objIdx].Size;
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
            objRadius = objScale * UnifyRadius;

            objPos[0] = table.transform.position[0] + UnityEngine.Random.Range(
                -(float)(_tableWidth / 3) + objRadius, (float)(_tableWidth / 3) - objRadius);
            objPos[1] = table.transform.position[1] + _tableHeight * 0.5F + objRadius;
            objPos[2] = table.transform.position[2] + UnityEngine.Random.Range(
                -(float)(_tableLength / 3) + objRadius, (float)(_tableLength / 3) - objRadius);

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