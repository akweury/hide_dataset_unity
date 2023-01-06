using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;
using Pngcs.Unity;


public partial class VisualObjs : MonoBehaviour
{
    // useful public variables
    public GameObject table; // https://www.cgtrader.com/items/1875799/download-page
    public List<GameObject> trainModels;
    public List<GameObject> testModels;
    public DepthCamera DepthCamera;

    // useful private variables
    private int _frames;
    private string _rootPath;
    private string _savePath;
    private int _ruleFileCounter;
    private Rules _rules;
    private int _fileCounter;
    private List<GameObject> _objInstances;
    private List<GameObject> _models;
    private List<MeshRenderer> _modelRenderers;
    private SceneStruct _sceneData;
    
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
    public RenderTexture texture;
    public GameObject cursor;


    public Light projector;
    public Shader depthShader;
    public Shader normalShader;

    private Camera cam;
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
        cam = Instantiate(Camera.main, Camera.main.transform.position, Camera.main.transform.rotation);
        cam.targetTexture = texture;


        RenderSettings.ambientLight = Color.gray;
        // table.GetComponent<GameObject>().enabled = true;
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
        _models = UpdateModels(_rules);

        // instantiate the table
        Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
            UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));

        table.transform.localScale = new Vector3(_tableLength, _tableHeight, _tableWidth);
        environmentMap.SetTexture("_Tex", danceRoomEnvironment);

        foreach (var model in _models)
        {
            _modelRenderers.Add(model.GetComponent<MeshRenderer>());
        }
    }

    // Update is called once per frame
    void Update()
    {
        // render the scene
        if (_frames % _frameLoop == 0)
        {
            _sceneData = RenderNewScene(_rules, _models);
        }

        // save the scene data if it is a new scene
        if (_frames % _frameLoop == _frameLoop - 1)
        {
            SaveScene(_objInstances, _sceneData);
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
            _models = UpdateModels(_rules);
        }

        // stop rendering when file number exceeded the threshold
        if (CheckFinished())
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
        _frames++;
    }

    SceneStruct RenderNewScene(Rules rules, List<GameObject> models)
    {
        SceneStruct sceneData = InitNewScene(_objInstances.Count, _fileCounter);
        // instantiate the objects on the table
        int objNum = rules.RandomObjPerScene + rules.RuleObjPerScene + rules.TargetObjPerScene;
        _objInstances = RenderObjs(objNum, rules, sceneData, models);
        return sceneData;
    }

    SceneStruct InitNewScene(int objNum, int sceneId)
    {
        SceneStruct newSceneData = new SceneStruct(objNum, sceneId);
        return newSceneData;
    }

    void SaveScene(List<GameObject> objs, SceneStruct sceneData)
    {
        Calibration camera0 = getCameraMatrix();
        DepthMap depthMap0 = CaptureDepth("depth0", objs);
        CaptureNormal("normal0", camera0.R, objs);
        CaptureScene("image");
        writeDataFileOneView("data0", camera0, depthMap0, objs, sceneData);
        environmentMap.SetTexture("_Tex", danceRoomEnvironment);
        DestroyObjs(objs);
    }

    List<GameObject> UpdateModels(Rules rules)
    {
        if (_fileCounter >= rules.TrainNum + rules.ValNum)
        {
            _sceneType = "test";
            _savePath = _rootPath + "test/";
            return testModels;
        }

        // generate validation scenes
        if (_fileCounter >= rules.TrainNum)
        {
            _sceneType = "val";
            _savePath = _rootPath + "val/";
            return testModels;
        }

        // generate training scenes
        if (_fileCounter >= 0)
        {
            _sceneType = "train";
            _savePath = _rootPath + "train/";
            return trainModels;
        }

        return null;
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


    SceneStruct PlaceObjs(Rules rules, List<GameObject> models, SceneStruct sceneData)
    {
        bool layoutFinished = false;
        while (!layoutFinished)
        {
            int objIdx = 0;

            // add target object
            sceneData = AddRuleObj(0, rules.target, models, sceneData);
            objIdx++;

            // add rule objects
            for (int i = 0; i < rules.RuleObjPerScene; i++)
            {
                sceneData = AddRuleObj(i + 1, rules.objs[i], models, sceneData);
                objIdx++;
            }

            // add random objects
            for (int i = objIdx; i < rules.RandomObjPerScene; i++)
            {
                sceneData = AddRandomObj(i, models, sceneData);
                objIdx++;
            }

            layoutFinished = CheckScene(sceneData);
        }

        return sceneData;
    }

    bool CheckScene(SceneStruct sceneData)
    {
        return true;
    }

    SceneStruct AddRuleObj(int objId, ObjProp objProp, List<GameObject> models, SceneStruct sceneData)
    {
        float newScale = strFloMapping[objProp.size];

        // generate random positions for test scenes
        if (String.Equals(_sceneType, "test"))
        {
            sceneData = RandomPos(newScale, objProp.Shape, objProp.Material, objId, sceneData);
            return sceneData;
        }

        int num_tries = 0;
        float obj_radius = newScale * UNIFY_RADIUS;
        GameObject obj_model = models[(int)strFloMapping[objProp.Shape]];
        Vector3 obj_pos;
        // find a 3D position for the new object
        while (true)
        {
            // exceed the maximum trying time
            num_tries += 1;
            if (num_tries > _maxNumTry) return sceneData;

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
                    objProp.x + sceneData.Objects[0].Position[0],
                    table.transform.position[1] + _tableHeight * 0.5F + obj_radius,
                    objProp.z + sceneData.Objects[0].Position[2]);

                // check if new position locates in the area of table
                if (obj_pos[0] < -(float)(_tableWidth / 2) + obj_radius ||
                    obj_pos[0] > (float)(_tableWidth / 2) - obj_radius ||
                    obj_pos[2] < -(float)(_tableLength / 2) + obj_radius ||
                    obj_pos[2] > (float)(_tableLength / 2) - obj_radius
                   )
                {
                    pos_good = false;
                    return sceneData;
                }
            }

            if (pos_good) break;
        }

        // for cubes, adjust its radius
        if (objProp.Shape == "cube") obj_radius *= (float)Math.Sqrt(2);
        // Obj3D target3D = new Obj3D(obj_pos, obj_radius);

        // record the object data
        // obj3Ds.Add(target3D);
        sceneData.Objects[objId] = new ObjectStruct(objId, objProp.Shape, objProp.Material, obj_radius, obj_pos);
        return sceneData;
    }

    SceneStruct RandomPos(float newScale, string newShape, string newMaterial, int objIdx, SceneStruct sceneData)
    {
        int numTries = 0;
        Vector3 objPos = new Vector3();
        float objRadius = newScale * UNIFY_RADIUS;
        while (true)
        {
            // check if exceed the maximum trying time
            numTries += 1;
            if (numTries > _maxNumTry) return sceneData;

            // choose new size and position


            objPos[0] = table.transform.position[0] + UnityEngine.Random.Range(
                -(float)(_tableWidth / 2) + objRadius, (float)(_tableWidth / 2) - objRadius);
            objPos[1] = table.transform.position[1] + _tableHeight * 0.5F + objRadius;
            objPos[2] = table.transform.position[2] + UnityEngine.Random.Range(
                -(float)(_tableLength / 2) + objRadius, (float)(_tableLength / 2) - objRadius);

            // check for overlapping
            bool distGood = true;
            // bool margins_good = true;
            for (int i = 0; i < objIdx; i++)
            {
                float dx = objPos[0] - sceneData.Objects[i].Position[0];
                float dz = objPos[2] - sceneData.Objects[i].Position[2];
                float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                if (dist - objRadius - sceneData.Objects[i].Size < MINIMUM_OBJ_DIST)
                {
                    // distGood = false;
                    return sceneData;
                }
            }

            break;
        }

        // for cubes, adjust its radius
        if (newShape == "cube") objRadius *= (float)Math.Sqrt(2);

        // record the object data
        sceneData.Objects[objIdx] = new ObjectStruct(objIdx, newShape, newMaterial, objRadius, objPos);

        return sceneData;
    }

    SceneStruct AddRandomObj(int objIdx, List<GameObject> models, SceneStruct sceneData)
    {
        int num_tries = 0;
        float new_scale;
        // choose a random new model
        GameObject newModel = models[UnityEngine.Random.Range(0, models.Count)];
        string shapeName = strStrMapping[newModel.name];
        float objRadius;
        Vector3 objPos = new Vector3();

        // find a 3D position for the new object
        while (true)
        {
            num_tries += 1;
            // exceed the maximum trying time
            if (num_tries > _maxNumTry) return sceneData;

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
                    return sceneData;
                }
            }

            if (dists_good) break;
        }

        // create the new object
        // GameObject objInst = NewObjectInstantiate(obj_radius * 2, obj_pos, new_model, "");

        // for cubes, adjust its radius
        if (shapeName == "cube") objRadius *= (float)Math.Sqrt(2);

        // record the object data
        sceneData.Objects[objIdx] = new ObjectStruct(objIdx, shapeName, "", objRadius, objPos);
        return sceneData;
    }

    List<GameObject> RenderObjs(int objNum, Rules rules, SceneStruct sceneData, List<GameObject> models)
    {
        sceneData = PlaceObjs(rules, models, sceneData);

        List<GameObject> objs = new List<GameObject>(new GameObject[objNum]);
        for (int i = 0; i < sceneData.Objects.Count; i++)
        {
            float objSize = sceneData.Objects[i].Size;
            // for cubes, adjust its radius
            if (sceneData.Objects[i].Shape == "cube") objSize = sceneData.Objects[i].Size / (float)Math.Sqrt(2);

            int modelId = (int)strFloMapping[sceneData.Objects[i].Shape];
            objs[i] = NewObjectInstantiate(objSize * 2, sceneData.Objects[i].Position, models[modelId],
                _modelRenderers[modelId], sceneData.Objects[i].Material);
            // record the object data
            sceneData.Objects[i].Material = _modelRenderers[modelId].material.name.Replace(" (Instance)", "");
        }

        return objs;
    }


    GameObject NewObjectInstantiate(float scale, Vector3 newPoint, GameObject newModel, MeshRenderer newModelRenderer,
        string material)
    {
        // place the object on the table
        Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
            UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));
        Vector3 position = new Vector3(newPoint.x, newPoint.y, newPoint.z);
        GameObject objInst = Instantiate(newModel, position, rotation);

        if (material != "")
        {
            newModelRenderer.material = materials[(int)strFloMapping[material]];
        }
        else
        {
            newModelRenderer.material = materials[UnityEngine.Random.Range(0, materials.Count - 1)];
        }

        objInst.name = newModel.name;
        objInst.transform.localScale = new Vector3(scale, scale, scale);

        return objInst;
    }

    void CaptureScene(string name)
    {
        RenderTexture.active = cam.targetTexture;
        // projector.cookie = white;

        cam.Render();
        Texture2D currentTexture =
            new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGB24, false);
        currentTexture.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        currentTexture.Apply();

        int w = currentTexture.width;
        int h = currentTexture.height;
        Color[] imageGray = new Color[w * h];
        Color[] imageRGB = new Color[w * h];
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                UnityEngine.Color c = currentTexture.GetPixel(j, i);
                float gray_val = (c.r + 2 * c.g + c.b) / 4;
                imageGray[i * w + j] = new UnityEngine.Color(gray_val, 0, 0, 0);
                imageRGB[i * w + j] = new UnityEngine.Color(c.r, c.g, c.b, 0);
            }
        }

        PNG.Write(imageRGB, w, h, 8, false, false,
            _savePath + _ruleFileCounter.ToString("D2") + "." + _fileCounter.ToString("D5") + "." + name + ".png");
        //PNG.Write(imageRGB, w, h, 8, false, true, saved_path + fileCounter.ToString("D5") + "." + name + "RGB.png");
        Destroy(currentTexture);
    }


    DepthMap CaptureDepth(string name, List<GameObject> objs)
    {
        RenderTexture.active = cam.targetTexture;
        // Texture env = environmentMap.GetTexture("_Tex");
        // environmentMap.SetTexture("_Tex", blackEnvironment);

        cam.RenderWithShader(depthShader, "RenderType");
        Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGBAFloat,
            false);
        image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        image.Apply();

        for (int i = 0; i < objs.Count; i++)
        {
            objs[i].SetActive(false);
        }

        cam.RenderWithShader(depthShader, "RenderType");
        Texture2D spinCubeImage = new Texture2D(cam.targetTexture.width, cam.targetTexture.height,
            TextureFormat.RGBAFloat, false);
        spinCubeImage.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        spinCubeImage.Apply();

        for (int i = 0; i < objs.Count; i++)
        {
            objs[i].SetActive(true);
        }

        DepthMap depthMap = new DepthMap(cam.targetTexture.width, cam.targetTexture.height);

        int w = image.width;
        int h = image.height;
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                UnityEngine.Color c = image.GetPixel(j, i);
                UnityEngine.Color s = spinCubeImage.GetPixel(j, i);
                float dz = (float)(1 - c.r) * (float)((float)cam.farClipPlane);
                float dx = (float)((float)(w + 1) / 2 - (float)j - (float)1) / (float)cam.focalLength * dz;
                float dy = (float)((float)(h + 1) / 2 - (float)i - (float)1) / (float)cam.focalLength * dz;
                float depth = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
                if (c.r == 0)
                    depth = 0;
                if (s.r != 0 && s.r == c.r)
                    depth = 0; // filter out spin cube
                depthMap.depths[i * w + j] = new UnityEngine.Color(depth, 0, 0, 0);
                if (depth != 0)
                {
                    if (depth > depthMap.maxDepth)
                        depthMap.maxDepth = depth;
                    if (depth < depthMap.minDepth)
                        depthMap.minDepth = depth;
                }
            }
        }

        for (int i = 0; i < h * w; i++)
        {
            if (depthMap.depths[i].r != 0)
                depthMap.depths[i].r =
                    (depthMap.depths[i].r - depthMap.minDepth) /
                    (depthMap.maxDepth - depthMap.minDepth); // normalise all the R-channel values in range [0,1]
        }

        PNG.Write(depthMap.depths, w, h, 16, false, true,
            _savePath + _ruleFileCounter.ToString("D2") + "." + _fileCounter.ToString("D5") + "." + name + ".png");

        Destroy(image);
        Destroy(spinCubeImage);

        return depthMap;
    }

    void CaptureNormal(string name, Matrix4x4 R, List<GameObject> objs)
    {
        RenderTexture.active = cam.targetTexture;
        // Texture env = environmentMap.GetTexture("_Tex");
        // environmentMap.SetTexture("_Tex", blackEnvironment);

        cam.RenderWithShader(normalShader, "RenderType");
        Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGBAFloat,
            false);
        image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        image.Apply();

        for (int i = 0; i < objs.Count; i++)
        {
            objs[i].SetActive(false);
        }

        cam.RenderWithShader(normalShader, "RenderType");
        Texture2D cubeImage = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGBAFloat,
            false);
        cubeImage.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        cubeImage.Apply();
        for (int i = 0; i < objs.Count; i++)
        {
            objs[i].SetActive(true);
        }

        // environmentMap.SetTexture("_Tex", env);
        Color[] normalMap = new Color[cam.targetTexture.width * cam.targetTexture.height];

        int w = image.width;
        int h = image.height;
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                UnityEngine.Color c = image.GetPixel(j, i);
                UnityEngine.Color s = cubeImage.GetPixel(j, i);
                // filter out the cube part
                if (s.a != 0 && s.r == c.r && s.g == c.g && s.b == c.b)
                {
                    c.a = 0;
                    c.r = 0;
                    c.g = 0;
                    c.b = 0;
                }

                if (c.a != 0)
                    c.g = 1 - c.g;

                normalMap[i * w + j] = c;
            }
        }

        PNG.Write(normalMap, w, h, 8, false, false,
            _savePath + _ruleFileCounter.ToString("D2") + "." + _fileCounter.ToString("D5") + "." + name + ".png");

        Destroy(image);
        Destroy(cubeImage);
    }

    void writeDataFileOneView(string name, Calibration camera, DepthMap depthMap, List<GameObject> objs,
        SceneStruct sceneData)
    {
        StreamWriter writer =
            new StreamWriter(
                _savePath + _ruleFileCounter.ToString("D2") + "." + _fileCounter.ToString("D5") + "." + name + ".json",
                false);
        String data1 = "{" + "\"K\":[[" +
                       camera.K[0, 0] + "," + 0 + "," +
                       camera.K[0, 2].ToString().Replace(',', '.') + "],[" + 0 + "," +
                       camera.K[1, 1] + "," +
                       camera.K[0, 2].ToString().Replace(',', '.') + "],[" + 0 + "," + 0 + "," + 1 + "]]" + "," +
                       "\"R\":[[" +
                       camera.R[0, 0].ToString().Replace(',', '.') + "," +
                       camera.R[0, 1].ToString().Replace(',', '.') + "," +
                       camera.R[0, 2].ToString().Replace(',', '.') + "],[" +
                       camera.R[1, 0].ToString().Replace(',', '.') + "," +
                       camera.R[1, 1].ToString().Replace(',', '.') + "," +
                       camera.R[1, 2].ToString().Replace(',', '.') + "],[" +
                       camera.R[2, 0].ToString().Replace(',', '.') + "," +
                       camera.R[2, 1].ToString().Replace(',', '.') + "," +
                       camera.R[2, 2].ToString().Replace(',', '.') + "]]" + "," + "\"t\":[" +
                       camera.t[0].ToString().Replace(',', '.') + "," +
                       camera.t[1].ToString().Replace(',', '.') + "," +
                       camera.t[2].ToString().Replace(',', '.') + "]" + "," + "\"minDepth\":" +
                       depthMap.minDepth.ToString().Replace(',', '.') + "," + "\"maxDepth\":" +
                       depthMap.maxDepth.ToString().Replace(',', '.') + ",";


        String objectData = "\"objects\":[";
        for (int i = 0; i < objs.Count; i++)
        {
            objectData += "{" +
                          "\"id\":" + sceneData.Objects[i].Id + "," +
                          "\"shape\":\"" + sceneData.Objects[i].Shape + "\"" + "," +
                          "\"size\":\"" + sceneData.Objects[i].Size + "\"" + "," +
                          "\"material\":\"" + sceneData.Objects[i].Material + "\"" + "," +
                          "\"position\":[" +
                          (float)sceneData.Objects[i].Position[0] + "," +
                          (float)sceneData.Objects[i].Position[1] + "," +
                          (float)sceneData.Objects[i].Position[2] +
                          "]" +
                          "}";
            if (i != objs.Count - 1)
            {
                objectData += ",";
            }
        }

        objectData += "]";

        writer.Write(data1 + objectData + "}");
        writer.Close();
    }

    Calibration getCameraMatrix()
    {
        Matrix4x4 R = Matrix4x4.Rotate(Quaternion.Inverse(cam.transform.rotation));
        R[1, 0] = -R[1, 0];
        R[1, 1] = -R[1, 1];
        R[1, 2] = -R[1, 2];
        Vector3 t = R * -cam.transform.position;
        R[0, 2] = -R[0, 2];
        R[1, 2] = -R[1, 2];
        R[2, 2] = -R[2, 2];
        Matrix4x4 K = Matrix4x4.identity;
        K[0, 0] = cam.focalLength;
        K[1, 1] = cam.focalLength;
        K[0, 2] = (float)(cam.targetTexture.width + 1) / 2;
        K[1, 2] = (float)(cam.targetTexture.height + 1) / 2;

        Calibration camera = new Calibration(K, R, t);

        return camera;
    }

    Calibration getProjectorMatrix()
    {
        Matrix4x4 R = Matrix4x4.Rotate(Quaternion.Inverse(projector.transform.rotation));
        R[1, 0] = -R[1, 0];
        R[1, 1] = -R[1, 1];
        R[1, 2] = -R[1, 2];
        // Vector3 t = R * -cam.transform.position;
        Vector3 t = projector.transform.position;
        R[0, 2] = -R[0, 2];
        R[1, 2] = -R[1, 2];
        R[2, 2] = -R[2, 2];
        Matrix4x4 K = Matrix4x4.identity;

        Calibration projectorCalibration = new Calibration(K, R, t);

        return projectorCalibration;
    }
}