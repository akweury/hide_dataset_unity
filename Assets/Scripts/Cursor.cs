using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Cursor : MonoBehaviour
{
    static int OBJ_NUM = 3;
    int MAX_NUM_TRIES = 50;
    float TABLE_WIDTH_BASE = 1.5F;
    float TABLE_LENGTH_BASE = 1.5F;
    float TABLE_HEIGHT_BASE = 0.1F;
    float UNIFY_RADIUS = 0.5F;
    float MINIMUM_OBJ_DIST = (float)0.1;
    private float MINIMUM_SCALE_RANGE = 0.15F;
    private float MAXIMUM_SCALE_RANGE = 0.4F;

    public List<GameObject> train_models;
    public List<GameObject> test_models;
    public List<Material> materials;
    public GameObject table;

    public Transform groundCheckTransform;
    private bool jumpKeyWasPressed;
    private float horizontalInput;
    private float verticalInput;
    private Rigidbody rigidbodyComponent;
    private bool isGrounded;
    private List<GameObject> models;

    List<GameObject> modelInsts = new List<GameObject>(new GameObject[OBJ_NUM]);
    private SceneStruct SceneData;
    float table_length;
    float table_width;
    float table_height;
    float scale_factor = 1;

    public struct Obj3D
    {
        public Vector3 p;
        public float radius;
    }

    public struct ObjectStruct
    {
        public int Id;
        public string Shape;
        public float Size;
        public Vector3 Position;
    }

    public class SceneStruct
    {
        public int ImageIndex;
        public List<ObjectStruct> Objects;

        public SceneStruct(int objNum, int imageIndex)
        {
            Objects = new List<ObjectStruct>(new ObjectStruct[objNum]);
            ImageIndex = imageIndex;
        }
    }


    List<GameObject> addRandomObjs()
    {
        List<GameObject> objInsts = new List<GameObject>(new GameObject[OBJ_NUM]);
        List<Obj3D> obj3Ds = new List<Obj3D>();
        for (int i = 0; i < OBJ_NUM; i++)
        {
            int num_tries = 0;
            float new_scale;
            Obj3D new_obj_3D;

            // choose a random new model
            int objIdx = UnityEngine.Random.Range(0, OBJ_NUM);
            GameObject new_model = models[UnityEngine.Random.Range(0, models.Count)];

            // find a 3D position for the new object
            while (true)
            {
                num_tries += 1;
                // exceed the maximum trying time
                if (num_tries > MAX_NUM_TRIES) return addRandomObjs();
                // choose new size and position
                new_scale = UnityEngine.Random.Range(MINIMUM_SCALE_RANGE, MAXIMUM_SCALE_RANGE) * scale_factor;
                new_obj_3D.radius = new_scale * UNIFY_RADIUS;

                new_obj_3D.p.x = table.transform.position[0] + UnityEngine.Random.Range(
                    -(float)(table_width / 2) + new_obj_3D.radius, (float)(table_width / 2) - new_obj_3D.radius);
                new_obj_3D.p.y = table.transform.position[1] + table_height * 0.5F + new_obj_3D.radius;
                new_obj_3D.p.z = table.transform.position[2] + UnityEngine.Random.Range(
                    -(float)(table_length / 2) + new_obj_3D.radius, (float)(table_length / 2) - new_obj_3D.radius);

                // check for overlapping
                bool dists_good = true;
                // bool margins_good = true;
                for (int j = 0; j < obj3Ds.Count; j++)
                {
                    float dx = new_obj_3D.p.x - obj3Ds[j].p.x;
                    float dz = new_obj_3D.p.z - obj3Ds[j].p.z;
                    float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                    if (dist - new_obj_3D.radius - obj3Ds[j].radius < MINIMUM_OBJ_DIST)
                    {
                        dists_good = false;
                        break;
                    }
                }

                if (dists_good) break;
            }

            // create the new object
            objInsts[i] = NewObjectInstantiate(new_obj_3D.radius * 2, new_obj_3D.p, new_model);

            // for cubes, adjust its radius
            if (objInsts[i].name == "Cube") new_obj_3D.radius *= (float)Math.Sqrt(2);

            // record the object data
            obj3Ds.Add(new_obj_3D);
            ObjectStruct objData;
            objData.Id = i;
            objData.Shape = new_model.name;
            objData.Size = new_obj_3D.radius;
            objData.Position = objInsts[i].transform.position;
            // SceneData.Objects[i] = objData;
        }

        return objInsts;
    }

    GameObject NewObjectInstantiate(float scale, Vector3 newPoint, GameObject new_model)
    {
        Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range((float)0, (float)0),
            UnityEngine.Random.Range((float)-150, (float)0), UnityEngine.Random.Range((float)-0, (float)0));

        // random place the object on the table
        Vector3 position = new Vector3(newPoint.x, newPoint.y, newPoint.z);
        GameObject objInst = Instantiate(new_model, position, rotation);
        Renderer[] children = objInst.GetComponentsInChildren<MeshRenderer>();
        foreach (Renderer rend in children)
        {
            rend.material = materials[UnityEngine.Random.Range(0, materials.Count)];
        }

        // objInst.GetComponents<MeshRenderer>().material = materials[UnityEngine.Random.Range(0, materials.Count)];
        // GameObject objInst = Instantiate(models[objIdx], new Vector3(1.5f, -4f, 18f), rotation);
        objInst.name = new_model.name;
        objInst.transform.localScale = new Vector3(scale, scale, scale);

        return objInst;
    }


    // Start is called before the first frame update
    void Start()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
        models = train_models;
        scale_factor = 4F;
        table_length = TABLE_LENGTH_BASE * scale_factor;
        table_width = TABLE_WIDTH_BASE * scale_factor;
        table_height = TABLE_HEIGHT_BASE * scale_factor;

        // generate a set of objects (cubes, spheres)
        modelInsts = addRandomObjs();
    }

    // Update is called once per frame
    void Update()
    {
        // check if space key is pressed down
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpKeyWasPressed = true;
        }

        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
    }

    // called once every physic update
    private void FixedUpdate()
    {
        rigidbodyComponent.velocity = new Vector3(horizontalInput, rigidbodyComponent.velocity.y, verticalInput);

        if (Physics.OverlapSphere(groundCheckTransform.position, 0.1F).Length == 0)
        {
            return;
        }

        if (jumpKeyWasPressed)
        {
            Debug.Log("Space key was pressed down.");
            rigidbodyComponent.AddForce(Vector3.up * 5, ForceMode.VelocityChange);
            jumpKeyWasPressed = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}