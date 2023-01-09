using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.SearchService;
using UnityEngine;
using utils;

public class SceneGenerator : MonoBehaviour
{
    // useful public variables
    public GameObject table; // https://www.cgtrader.com/items/1875799/download-page
    public List<GameObject> sphereModels;
    public List<GameObject> cubeModels;
    public RenderTexture texture;
    public Shader depthShader;
    public Shader normalShader;

    // useful private variables
    private string _filePrefix;
    private int _frames;
    private string _outputPath;
    private string _inputPath;
    // private int _ruleFileCounter;
    private int _fileCounter;
    private List<GameObject> _objInstances; 
    private List<MeshRenderer> _modelRenderers;
    private List<ObjectStruct> _sceneData;
    private DepthCamera _depthCamera;
    private const float TableWidthBase = 1.5F;
    private const float TableLengthBase = 1.5F;
    private const float TableHeightBase = 0.1F;
    private const float ScaleFactor = 4F;
    private const int FrameLoop = 10;
    private float _tableLength;
    private float _tableWidth;
    private float _tableHeight;
    private FileInfo[] _files;
    private string _sceneType;
    private SceneJson _sceneJson;

    private bool _sceneDone;
    // Start is called before the first frame update
    void Start()
    {
        Camera cam = Instantiate(Camera.main, Camera.main.transform.position, Camera.main.transform.rotation);
        _depthCamera = new DepthCamera(cam, depthShader, normalShader);
        _depthCamera.Cam.targetTexture = texture;
        RenderSettings.ambientLight = Color.gray;

        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/output");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/../CapturedData/input");
        _inputPath = Application.dataPath + "/../CapturedData/input/";
        _outputPath = Application.dataPath + "/../CapturedData/output/";

        // adjust table size based on camera sensor size
        // _scaleFactor = 4F;
        _tableLength = TableLengthBase * ScaleFactor;
        _tableWidth = TableWidthBase * ScaleFactor;
        _tableHeight = TableHeightBase * ScaleFactor;

        // get all json files
        DirectoryInfo d = new DirectoryInfo(_inputPath);
        _files = d.GetFiles("*.json"); // Getting Rule files
        _fileCounter = 0;

        // load a new scene
        _sceneJson = LoadNewScene(_files[_fileCounter].FullName);
        _filePrefix = _outputPath + "Test_output_" + _fileCounter;
        _fileCounter++;

        // instantiate the table
        table.transform.localScale = new Vector3(_tableLength, _tableHeight, _tableWidth);
    }

    // Update is called once per frame
    void Update()
    {
        // render the scene
        if (_frames % FrameLoop == 0)
        {
            RenderNewScene(_sceneJson, sphereModels, cubeModels, _sceneType);
        }

        // save the scene data if it is a new scene
        if (_frames % FrameLoop == FrameLoop - 1)
        {
            
            DepthCamera.SaveScene(_filePrefix, _objInstances, _depthCamera, _sceneData);
            DestroyObjs(_objInstances);
            _sceneDone = true;
            _filePrefix = _outputPath + "Test_output_" + _fileCounter;
            _fileCounter++;
        }


        // load a new rule file
        if (_sceneDone)
        {
            DestroyObjs(_objInstances);
            if (_fileCounter >= _files.Length)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
            else
            {
                _sceneJson = LoadNewScene(_files[_fileCounter].FullName);
                _sceneDone = false;
                _fileCounter++;
            }
        }

        // stop rendering when file number exceeded the threshold
        _frames++;
    }

    SceneJson LoadNewScene(string sceneFileName)
    {
        StreamReader streamReader = new StreamReader(sceneFileName);
        string json = streamReader.ReadToEnd();
        SceneJson sceneJson = JsonConvert.DeserializeObject<SceneJson>(json);
        return sceneJson;
    }


    void RenderNewScene(SceneJson sceneJson, List<GameObject> spheres, List<GameObject> cubes, string sceneType)
    {
        int objNum = sceneJson.Objs.Count;
        _sceneData = new List<ObjectStruct>(new ObjectStruct[objNum]);
        _objInstances = new List<GameObject>(new GameObject[objNum]);

        _sceneData = FillSceneData(sceneJson, _sceneData);
        
        int objId = 0;
        foreach (var sceneObj in _sceneData)
        {
            float objSize = sceneObj.Size;

            // for cubes, adjust its radius
            if (sceneObj.Shape == "cube") objSize = sceneObj.Size / (float)Math.Sqrt(2);

            // match the obj with a GameObject
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

    List<ObjectStruct> FillSceneData(SceneJson sceneJson, List<ObjectStruct> sceneData)
    {
        foreach (var sceneObj in sceneJson.Objs)
        {
            int objId = sceneObj.Id;
            foreach (var configObj in sceneJson.SceneConfig.Objects)
            {
                if (configObj.Id == objId)
                {
                    string shape = configObj.Shape;
                    string material = configObj.Material;
                    float size = configObj.Size;
                    float[] position = configObj.Pos;
                    sceneData[objId] = new ObjectStruct(objId, shape, material, size)
                    {
                        Position = new Vector3(position[0],position[1], position[2])
                    };
                }
            }
        }

        return sceneData;
    }
    
    GameObject NewObjectInstantiate(float scale, Vector3 newPoint, GameObject newModel)
    {
        // place the object on the table
        Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
            UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));
        Vector3 position = new Vector3(newPoint.x, newPoint.y, newPoint.z);
        GameObject objInst = Instantiate(newModel, position, rotation);
        
        objInst.name = newModel.name;
        objInst.transform.localScale = new Vector3(scale, scale, scale);

        return objInst;
    }
 
    void DestroyObjs(List<GameObject> objs)
    {
        foreach (var obj in objs)
        {
            if (obj) Destroy(obj);
        }
    }
    


    
}