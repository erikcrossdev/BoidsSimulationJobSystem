using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.InputSystem.Controls;

[BurstCompile] //translate .NET to a highly optimized native code using LLVM
public struct ControlBoidJobs : IJobParallelForTransform
{
    //read only lets you mark a member allow us to schedule two jobs referencing the same container simutaneously
    [ReadOnly] public float3 PlayerPos;
    [ReadOnly] public float DeltaTime;
    [ReadOnly] public float DistanceThreshold;
    [ReadOnly] public float MoveSpeed;
	public void Execute(int index, TransformAccess transform)
	{
        //face player
		float3 direction = math.normalize(PlayerPos - (float3)transform.position);
        float angle = math.atan2(direction.y, direction.x);
        angle -= math.radians(90f);

        quaternion lookRot = quaternion.AxisAngle(new float3(0, 0, 1), angle);
        transform.rotation = lookRot;
        //move away
        float distance = math.distance(PlayerPos, transform.position);
        float3 newPosition = (float3)transform.position - direction * DeltaTime * MoveSpeed;
        float3 maskedPosition = math.select(transform.position, newPosition, distance<= DistanceThreshold);
	    transform.position = maskedPosition;
    }
}

[BurstCompile]
public class BoidsSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _boidPrefab;
    [SerializeField] private int _numberOfBoidsToSpawn;
    [SerializeField] private float _spawnRadius = 15f;
    [SerializeField] private Transform _playerPos;

    [Header("Boids")]
    [SerializeField] private float _distanceThreshold = 5f;
	[SerializeField] private float _moveSpeed;

    private bool _isDone;

    private TransformAccessArray _accessArray;
    private Transform[] _boidTransform;

    private List<GameObject> _boids = new List<GameObject>();
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        _boidTransform = new Transform[_numberOfBoidsToSpawn];
        _playerPos = PlayerBehaviour.instance.transform;

        for (int i = 0; i < _numberOfBoidsToSpawn; i++)
        {
            Vector2 spawnPosition = UnityEngine.Random.insideUnitCircle * _spawnRadius;
            GameObject go = Instantiate(_boidPrefab, spawnPosition, Quaternion.identity);
            _boids.Add(go);
            _boidTransform[i] = go.transform;
        }
        _accessArray = new TransformAccessArray(_boidTransform);
        _isDone = true;
    }

    void Update()
    {
        if (!_isDone) return;
        ControlBoidJobs job = new ControlBoidJobs()
        {
            PlayerPos = _playerPos.position,
            DistanceThreshold = _distanceThreshold,
            MoveSpeed = _moveSpeed,
            DeltaTime = Time.deltaTime
        };

        JobHandle jobHandle = job.Schedule(_accessArray);
		jobHandle.Complete();
        /*
         * without jobs
        //Calculating like this is better, so we will not have to much monobehaviours
        for (int i = 0; i < _boids.Count; i++)
        {
			Vector2 direction = (_playerPos.position - _boids[i].transform.position).normalized; //direction 
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; //angle 
			Quaternion lookRot = Quaternion.AngleAxis(angle, Vector3.forward); //transform to a look direction
			_boids[i].transform.rotation = lookRot;
			float distance = Vector2.Distance(_playerPos.position, _boids[i].transform.position);
			if (distance < _distanceThreshold)
			{
				_boids[i].transform.position += -(Vector3)direction * Time.deltaTime * _moveSpeed;

			}
		}*/
    }
}
