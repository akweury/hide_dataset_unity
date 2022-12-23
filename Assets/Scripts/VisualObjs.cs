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
    private int _objNum;
    private static int _frameLoop = 10;
    int _maxNumTries = 50;
    float TABLE_WIDTH_BASE = 1.5F;
    float TABLE_LENGTH_BASE = 1.5F;
    float TABLE_HEIGHT_BASE = 0.1F;
    float UNIFY_RADIUS = 0.5F;
    float MINIMUM_OBJ_DIST = (float)0.1;
    private float MINIMUM_SCALE_RANGE = 0.15F;
    private float MAXIMUM_SCALE_RANGE = 0.3F;

    public int single_idx = 1;
    public List<GameObject> train_models;
    public List<GameObject> test_models;
    public List<Material> materials;
    public SceneStruct SceneData;
    public GameObject table; // https://www.cgtrader.com/items/1875799/download-page
    public Material environmentMap;
    public Cubemap danceRoomEnvironment;
    public List<Cubemap> environments;
    public RenderTexture texture;
    public GameObject cursor;

    // public Light dirLight;
    public Light projector;

    // public Texture2D phaseH;
    // public Texture2D phaseV;
    // public Texture2D white;
    public Shader depthShader;

    // public Shader phaseShader;
    public Shader normalShader;


    private Camera cam;
    public GameObject tableModel;

    private List<GameObject> objInsts;
    private GameObject tableInst;


    private int frames = 0;

    // int lightSceneNum = 1;
    float table_length;
    float table_width;
    float table_height;
    int fileCounter;
    int maxFileCounter;

    private int _trainNum;
    private int _testNum;
    private int _valNum;


    int modelIdx = -1;
    float scale_factor = 1;

    string file_type;
    bool newScene;
    string saved_path;
    string root_path;

    List<GameObject> models;
    private Rules rulesJson;
    private string sceneType;
    private FileInfo[] _files;
    private int _ruleFileCounter;
    private bool initialize = false;


    // Start is called before the first frame update
    void Start()
    {
        cam = Instantiate(Camera.main, Camera.main.transform.position, Camera.main.transform.rotation);
        cam.targetTexture = texture;


        RenderSettings.ambientLight = UnityEngine.Color.gray;
        // table.GetComponent<GameObject>().enabled = true;
        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic/train");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic/val");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/data_synthetic/test");

        root_path = Application.dataPath + "/../CapturedData/data_synthetic/";
        // file_type = "synthetic";
        models = train_models;

        // adjust table size based on camera sensor size
        scale_factor = 4F;
        table_length = TABLE_LENGTH_BASE * scale_factor;
        table_width = TABLE_WIDTH_BASE * scale_factor;
        table_height = TABLE_HEIGHT_BASE * scale_factor;

        // _ruleFileCounter = 0;
        DirectoryInfo d = new DirectoryInfo(Application.dataPath + "/Scripts/Rules");
        _files = d.GetFiles("*.json"); //Getting Text files
    }

    // Update is called once per frame
    void Update()
    {
        // stop rendering when file number exceeded the threshold
        if (fileCounter >= _trainNum + _testNum + _valNum)
        {
            fileCounter = 0;
            initialize = false;
            if (_ruleFileCounter >= _files.Length)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                initialize = true;
            }
        }

        if (!initialize)
        {
            for (int i = 0; i < _objNum; i++)
            {
                if (objInsts[i] != null) Destroy(objInsts[i]);
            }

            StreamReader streamReader = new StreamReader(_files[_ruleFileCounter].FullName);
            string json = streamReader.ReadToEnd();
            rulesJson = JsonConvert.DeserializeObject<Rules>(json);
            if (rulesJson != null)
            {
                _objNum = rulesJson.RandomObjPerScene + rulesJson.RuleObjPerScene + rulesJson.TargetObjPerScene;
                objInsts = new List<GameObject>(new GameObject[_objNum]);
            }

            _trainNum = rulesJson.TrainNum;
            _valNum = rulesJson.ValNum;
            _testNum = rulesJson.TestNum;
            maxFileCounter = _trainNum + _valNum + _testNum;
            _ruleFileCounter += 1;
            initialize = true;
        }


        // generate test scenes
        if (fileCounter >= _trainNum + _valNum)
        {
            sceneType = "test";
            saved_path = root_path + "test/";
            models = test_models;
        }
        // generate validation scenes
        else if (fileCounter >= _trainNum)
        {
            sceneType = "val";
            models = test_models;
            saved_path = root_path + "val/";
        }

        // generate training scenes
        else if (fileCounter >= 0)
        {
            sceneType = "train";
            saved_path = root_path + "train/";
        }

        // save the scene data if it is a new scene
        if (frames % _frameLoop == _frameLoop - 1 && newScene)
        {
            Calibration camera0 = getCameraMatrix();
            DepthMap depthMap0 = CaptureDepth("depth0");
            CaptureNormal("normal0", camera0.R);
            CaptureScene("image");
            writeDataFileOneView("data0", objInsts, camera0, depthMap0);
            environmentMap.SetTexture("_Tex", danceRoomEnvironment);
            newScene = false;
            fileCounter++;
        }

        // render the scene
        if (frames % _frameLoop == 0 && fileCounter < maxFileCounter)
        {
            // change configurations if necessary
            SceneData = new SceneStruct(_objNum, fileCounter);

            for (int i = 0; i < _objNum; i++)
            {
                if (objInsts[i] != null) Destroy(objInsts[i]);
            }

            if (tableInst != null) Destroy(tableInst);

            int envIdx = new System.Random().Next(0, environments.Count);
            environmentMap.SetTexture("_Tex", danceRoomEnvironment);
            if (frames % _frameLoop == 0) modelIdx = (modelIdx + 1) % models.Count;

            // instantiate the table
            Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
                UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));

            Vector3 scale = new Vector3(table_length, table_height, table_width);
            table.transform.localScale = scale;

            // instantiate the objects on the table
            bool placeLayoutFinished = false;
            while (!placeLayoutFinished)
            {
                placeLayoutFinished = placeObjsRandomly();
            }
            renderObjs();
            newScene = true;
        }

        frames++;
    }

    bool placeObjsRandomly()
    {
        // List<Vector3> objPlaces = new List<Vector3>(new Vector3[RANDOM_OBJ_NUM]);
        int objIdx = 0;
        // add target object
        addRuleObj(0, rulesJson.target);
        objIdx++;
        // add rule objects
        for (int i = 0; i < rulesJson.objs.Count; i++)
        {
            bool objGood = addRuleObj(i + 1, rulesJson.objs[i]);
            objIdx++;
            if (!objGood)
            {
                return false;
            }
        }

        // add random objects
        for (int i = objIdx; i < _objNum; i++)
        {
            bool objGood = addRandomObj(i);
            objIdx++;
            if (!objGood)
            {
                return false;
            }
        }

        return true;
    }

    bool addRuleObj(int objId, ObjProp objProp)
    {
        float newScale = strFloMapping[objProp.size];

        if (String.Equals(sceneType, "test"))
        {
            bool isGood = randomPos(newScale, objProp.Shape, objProp.Material, objId);
            return isGood;
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
            if (num_tries > _maxNumTries) return false;

            // choose a proper position
            bool pos_good = true;
            // give a random position for the target object
            if (objProp.x == 0F && objProp.z == 0F)
            {
                obj_pos = new Vector3(
                    table.transform.position[0] +
                    UnityEngine.Random.Range(-(table_width / 2) + obj_radius, (table_width / 2) - obj_radius),
                    table.transform.position[1] + table_height * 0.5F + obj_radius,
                    table.transform.position[2] +
                    UnityEngine.Random.Range(-(table_length / 2) + obj_radius, (table_length / 2) - obj_radius));
            }
            // give a position for the rest rule objects based on target object position and info from the json file
            else
            {
                obj_pos = new Vector3(
                    objProp.x + SceneData.Objects[0].Position[0],
                    table.transform.position[1] + table_height * 0.5F + obj_radius,
                    objProp.z + SceneData.Objects[0].Position[2]);

                // check if new position locates in the area of table
                if (obj_pos[0] < -(float)(table_width / 2) + obj_radius ||
                    obj_pos[0] > (float)(table_width / 2) - obj_radius ||
                    obj_pos[2] < -(float)(table_length / 2) + obj_radius ||
                    obj_pos[2] > (float)(table_length / 2) - obj_radius
                   )
                {
                    pos_good = false;
                    return false;
                }
            }

            if (pos_good) break;
        }

        // for cubes, adjust its radius
        if (objProp.Shape == "cube") obj_radius *= (float)Math.Sqrt(2);
        // Obj3D target3D = new Obj3D(obj_pos, obj_radius);

        // record the object data
        // obj3Ds.Add(target3D);
        SceneData.Objects[objId] = new ObjectStruct(objId, objProp.Shape, objProp.Material, obj_radius, obj_pos);
        return true;
    }

    bool randomPos(float newScale, string newShape, string newMaterial, int objIdx)
    {
        int numTries = 0;
        Vector3 objPos = new Vector3();
        float objRadius = newScale * UNIFY_RADIUS;
        while (true)
        {
            // check if exceed the maximum trying time
            numTries += 1;
            if (numTries > _maxNumTries) return false;

            // choose new size and position


            objPos[0] = table.transform.position[0] + UnityEngine.Random.Range(
                -(float)(table_width / 2) + objRadius, (float)(table_width / 2) - objRadius);
            objPos[1] = table.transform.position[1] + table_height * 0.5F + objRadius;
            objPos[2] = table.transform.position[2] + UnityEngine.Random.Range(
                -(float)(table_length / 2) + objRadius, (float)(table_length / 2) - objRadius);

            // check for overlapping
            bool distGood = true;
            // bool margins_good = true;
            for (int i = 0; i < objIdx; i++)
            {
                float dx = objPos[0] - SceneData.Objects[i].Position[0];
                float dz = objPos[2] - SceneData.Objects[i].Position[2];
                float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                if (dist - objRadius - SceneData.Objects[i].Size < MINIMUM_OBJ_DIST)
                {
                    // distGood = false;
                    return false;
                }
            }

            break;
        }

        // for cubes, adjust its radius
        if (newShape == "cube") objRadius *= (float)Math.Sqrt(2);

        // record the object data
        SceneData.Objects[objIdx] = new ObjectStruct(objIdx, newShape, newMaterial, objRadius, objPos);

        return true;
    }

    bool addRandomObj(int objIdx)
    {
        int num_tries = 0;
        float new_scale;
        // choose a random new model
        GameObject new_model = models[UnityEngine.Random.Range(0, models.Count)];
        string shape_name = strStrMapping[new_model.name];
        float obj_radius;
        Vector3 obj_pos = new Vector3();

        // find a 3D position for the new object
        while (true)
        {
            num_tries += 1;
            // exceed the maximum trying time
            if (num_tries > _maxNumTries) return false;

            // choose new size and position
            new_scale = UnityEngine.Random.Range(MINIMUM_SCALE_RANGE, MAXIMUM_SCALE_RANGE) * scale_factor;
            obj_radius = new_scale * UNIFY_RADIUS;

            obj_pos[0] = table.transform.position[0] + UnityEngine.Random.Range(
                -(float)(table_width / 2) + obj_radius, (float)(table_width / 2) - obj_radius);
            obj_pos[1] = table.transform.position[1] + table_height * 0.5F + obj_radius;
            obj_pos[2] = table.transform.position[2] + UnityEngine.Random.Range(
                -(float)(table_length / 2) + obj_radius, (float)(table_length / 2) - obj_radius);

            // check for overlapping
            bool dists_good = true;
            // bool margins_good = true;
            for (int i = 0; i < objIdx; i++)
            {
                float dx = obj_pos[0] - SceneData.Objects[i].Position[0];
                float dz = obj_pos[2] - SceneData.Objects[i].Position[2];
                float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                if (dist - obj_radius - SceneData.Objects[i].Size < MINIMUM_OBJ_DIST)
                {
                    dists_good = false;
                    return false;
                }
            }

            if (dists_good) break;
        }

        // create the new object
        // GameObject objInst = NewObjectInstantiate(obj_radius * 2, obj_pos, new_model, "");

        // for cubes, adjust its radius
        if (shape_name == "cube") obj_radius *= (float)Math.Sqrt(2);

        // record the object data
        SceneData.Objects[objIdx] = new ObjectStruct(
            objIdx,
            shape_name,
            "",
            obj_radius,
            obj_pos);
        return true;
    }

    void renderObjs()
    {
        for (int i = 0; i < SceneData.Objects.Count; i++)
        {
            float objSize = SceneData.Objects[i].Size;
            // for cubes, adjust its radius
            if (SceneData.Objects[i].Shape == "cube") objSize = SceneData.Objects[i].Size / (float)Math.Sqrt(2);

            objInsts[i] = NewObjectInstantiate(
                objSize * 2,
                SceneData.Objects[i].Position,
                models[(int)strFloMapping[SceneData.Objects[i].Shape]],
                SceneData.Objects[i].Material);


            // record the object data
            SceneData.Objects[i].Material =
                objInsts[i].GetComponent<MeshRenderer>().material.name.Replace(" (Instance)", "");
        }
    }


    GameObject NewObjectInstantiate(float scale, Vector3 newPoint, GameObject new_model, string material)
    {
        Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
            UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));

        // random place the object on the table
        Vector3 position = new Vector3(newPoint.x, newPoint.y, newPoint.z);
        GameObject objInst = Instantiate(new_model, position, rotation);
        Renderer[] children = objInst.GetComponentsInChildren<MeshRenderer>();
        foreach (Renderer rend in children)
        {
            if (material != "")
            {
                rend.material = materials[(int)strFloMapping[material]];
            }
            else
            {
                rend.material = materials[UnityEngine.Random.Range(0, materials.Count - 1)];
            }
        }

        objInst.name = new_model.name;
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
            saved_path + _ruleFileCounter.ToString("D2") + "." + fileCounter.ToString("D5") + "." + name + ".png");
        //PNG.Write(imageRGB, w, h, 8, false, true, saved_path + fileCounter.ToString("D5") + "." + name + "RGB.png");
        Destroy(currentTexture);
    }


    DepthMap CaptureDepth(string name)
    {
        RenderTexture.active = cam.targetTexture;
        // Texture env = environmentMap.GetTexture("_Tex");
        // environmentMap.SetTexture("_Tex", blackEnvironment);

        cam.RenderWithShader(depthShader, "RenderType");
        Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGBAFloat,
            false);
        image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        image.Apply();

        for (int i = 0; i < _objNum; i++)
        {
            objInsts[i].SetActive(false);
        }

        cam.RenderWithShader(depthShader, "RenderType");
        Texture2D spinCubeImage = new Texture2D(cam.targetTexture.width, cam.targetTexture.height,
            TextureFormat.RGBAFloat, false);
        spinCubeImage.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        spinCubeImage.Apply();

        for (int i = 0; i < _objNum; i++)
        {
            objInsts[i].SetActive(true);
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
            saved_path + _ruleFileCounter.ToString("D2") + "." + fileCounter.ToString("D5") + "." + name + ".png");

        Destroy(image);
        Destroy(spinCubeImage);

        return depthMap;
    }

    void CaptureNormal(string name, Matrix4x4 R)
    {
        RenderTexture.active = cam.targetTexture;
        // Texture env = environmentMap.GetTexture("_Tex");
        // environmentMap.SetTexture("_Tex", blackEnvironment);

        cam.RenderWithShader(normalShader, "RenderType");
        Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGBAFloat,
            false);
        image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        image.Apply();

        for (int i = 0; i < _objNum; i++)
        {
            objInsts[i].SetActive(false);
        }

        cam.RenderWithShader(normalShader, "RenderType");
        Texture2D cubeImage = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGBAFloat,
            false);
        cubeImage.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        cubeImage.Apply();
        for (int i = 0; i < _objNum; i++)
        {
            objInsts[i].SetActive(true);
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
            saved_path + _ruleFileCounter.ToString("D2") + "." + fileCounter.ToString("D5") + "." + name + ".png");

        Destroy(image);
        Destroy(cubeImage);
    }

    void writeDataFileOneView(string name, List<GameObject> models, Calibration camera, DepthMap depthMap)
    {
        StreamWriter writer =
            new StreamWriter(
                saved_path + _ruleFileCounter.ToString("D2") + "." + fileCounter.ToString("D5") + "." + name + ".json",
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
        for (int i = 0; i < _objNum; i++)
        {
            objectData += "{" +
                          "\"id\":" + SceneData.Objects[i].Id + "," +
                          "\"shape\":\"" + SceneData.Objects[i].Shape + "\"" + "," +
                          "\"size\":\"" + SceneData.Objects[i].Size + "\"" + "," +
                          "\"material\":\"" + SceneData.Objects[i].Material + "\"" + "," +
                          "\"position\":[" +
                          (float)SceneData.Objects[i].Position[0] + "," +
                          (float)SceneData.Objects[i].Position[1] + "," +
                          (float)SceneData.Objects[i].Position[2] +
                          "]" +
                          "}";
            if (i != _objNum - 1)
            {
                objectData += ",";
            }
        }

        objectData += "]";

        writer.Write(data1 + objectData + "}");
        writer.Close();

        //writer.Write("{" + "\"K\":[[" +
        //    camera.K[0, 0] + "," + 0 + "," +
        //    camera.K[0, 2].ToString().Replace(',', '.') + "],[" + 0 + "," +
        //    camera.K[1, 1] + "," +
        //    camera.K[0, 2].ToString().Replace(',', '.') + "],[" + 0 + "," + 0 + "," + 1 + "]]" + "," + "\"R\":[[" +
        //    camera.R[0, 0].ToString().Replace(',', '.') + "," +
        //    camera.R[0, 1].ToString().Replace(',', '.') + "," +
        //    camera.R[0, 2].ToString().Replace(',', '.') + "],[" +
        //    camera.R[1, 0].ToString().Replace(',', '.') + "," +
        //    camera.R[1, 1].ToString().Replace(',', '.') + "," +
        //    camera.R[1, 2].ToString().Replace(',', '.') + "],[" +
        //    camera.R[2, 0].ToString().Replace(',', '.') + "," +
        //    camera.R[2, 1].ToString().Replace(',', '.') + "," +
        //    camera.R[2, 2].ToString().Replace(',', '.') + "]]" + "," + "\"t\":[" +
        //    camera.t[0].ToString().Replace(',', '.') + "," +
        //    camera.t[1].ToString().Replace(',', '.') + "," +
        //    camera.t[2].ToString().Replace(',', '.') + "]" + "," + "\"minDepth\":" +
        //    depthMap.minDepth.ToString().Replace(',', '.') + "," + "\"maxDepth\":" +
        //    depthMap.maxDepth.ToString().Replace(',', '.') + "," + "\"lightPos\":[" +
        //    projectorPos[0].ToString().Replace(',', '.') + "," +
        //    projectorPos[1].ToString().Replace(',', '.') + "," +
        //    projectorPos[2].ToString().Replace(',', '.') + "]" + "," + "\"name\":\""+
        //    model_name+"\"" + "}");
        //writer.Close();
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