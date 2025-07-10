using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class BoidsSpawnerAndBehaviour : MonoBehaviour
{
	[Header("Boids")]
	[SerializeField] private GameObject _boidPrefab;
	[SerializeField] private int _numberOfBoidsToSpawn = 100;
	[SerializeField] private float _spawnRadius = 15f;
	[SerializeField] private Sprite[] _sprites;
	[SerializeField] Transform _playerPosition;
	[SerializeField] private Vector2 boundsMin = new Vector2(-10, -5);
	[SerializeField] private Vector2 boundsMax = new Vector2(10, 5);
	[SerializeField] float _centerPullWeight = 1f;
	[SerializeField] bool _shouldAvoidPlayer = true;
	[SerializeField] float _lookAhead = 0.2f;

	[SerializeField] int _maxNeighbors = 15;

	[Header("Boid Behavior")]
	[SerializeField] private float _moveSpeed = 5f;
	[SerializeField] private float _neighborDistance = 3f;
	[SerializeField] private float _separationWeight = 1.5f;
	[SerializeField] private float _alignmentWeight = 1f;
	[SerializeField] private float _cohesionWeight = 1f;
	[SerializeField] private float _playerEffectWeight = 0.8f;

	public float CenterPullWeight => _centerPullWeight;
	public bool ShouldAvoidPlayer => _shouldAvoidPlayer;
	public float MoveSpeed => _moveSpeed;
	public float NeighborDistance => _neighborDistance;
	public float SeparationWeight => _separationWeight;
	public float AlignmentWeight => _alignmentWeight;
	public float CohesionWeight => _cohesionWeight;
	public float PlayerEffectWeight => _playerEffectWeight;

	public float LookAhead => _lookAhead;

	public int MaxNeighbors => _maxNeighbors;

	private TransformAccessArray _accessArray;
	private NativeArray<float3> _positions;
	private NativeArray<float3> _velocities;
	private NativeArray<float3> _newVelocities;

	[NativeDisableParallelForRestriction]
	private NativeArray<Unity.Mathematics.Random> _randoms;

	private SpriteRenderer[] _spriteRenderers;

	private List<GameObject> _boids = new List<GameObject>();

	public void OnChangeMoveSpeed(float param)
	{
		_moveSpeed = param;
	}
	public void OnChangeLookAhead(float param)
	{
		_lookAhead = param;
	}

	public void OnChangeMaxNeighbors(float param)
	{
		_maxNeighbors = (int)param;
	}
	public void OnChangeNeighborDistance(float param)
	{
		_neighborDistance = param;
	}

	public void OnChangeSeparationWeight(float param)
	{
		_separationWeight = param;
	}
	public void OnChangeAlignmentWeight(float param)
	{
		_alignmentWeight = param;
	}
	public void OnChangeCohesionWeight(float param)
	{
		_cohesionWeight = param;
	}
	public void OnChangePlayerEffectWeight(float param)
	{
		_playerEffectWeight = param;
	}
	public void OnChangeShouldAvoidPlayer(bool param)
	{
		_shouldAvoidPlayer = param;
	}
	public void OnChangeCenterPullWeight(float param)
	{
		_centerPullWeight = param;
	}

	private void GetCameraBounds()
	{
		Camera cam = Camera.main;
		float camHeight = 2f * cam.orthographicSize;
		float camWidth = camHeight * cam.aspect;

		boundsMin = new Vector2(-camWidth / 2f, -camHeight / 2f);
		boundsMax = new Vector2(camWidth / 2f, camHeight / 2f);
	}

	void Start()
	{
		_playerPosition = PlayerBehaviour.instance.transform;
		GetCameraBounds();
		//set randoms: 
		_randoms = new NativeArray<Unity.Mathematics.Random>(_numberOfBoidsToSpawn, Allocator.Persistent);
		uint seed = (uint)UnityEngine.Random.Range(1, 100000);
		for (int i = 0; i < _numberOfBoidsToSpawn; i++)
		{
			_randoms[i] = new Unity.Mathematics.Random(seed + (uint)i + 1);
		}

		Transform[] transforms = new Transform[_numberOfBoidsToSpawn];
		_spriteRenderers = new SpriteRenderer[_numberOfBoidsToSpawn];

		_positions = new NativeArray<float3>(_numberOfBoidsToSpawn, Allocator.Persistent);
		_velocities = new NativeArray<float3>(_numberOfBoidsToSpawn, Allocator.Persistent);
		_newVelocities = new NativeArray<float3>(_numberOfBoidsToSpawn, Allocator.Persistent);

		for (int i = 0; i < _numberOfBoidsToSpawn; i++)
		{
			Vector2 spawnPos = UnityEngine.Random.insideUnitCircle * _spawnRadius;
			GameObject boid = Instantiate(_boidPrefab, spawnPos, Quaternion.identity);
			var sprite = boid.GetComponent<SpriteRenderer>();
			sprite.sprite = _sprites[UnityEngine.Random.Range(0, _sprites.Length - 1)];
			_boids.Add(boid);
			transforms[i] = boid.transform;

			_positions[i] = new float3(spawnPos.x, spawnPos.y, 0);
			_velocities[i] = new float3(UnityEngine.Random.insideUnitCircle.normalized, 0);
			_spriteRenderers[i] = sprite;
		}

		_accessArray = new TransformAccessArray(transforms);
	}

	void Update()
	{
		// update current positions
		for (int i = 0; i < _accessArray.length; i++)
		{
			_positions[i] = _accessArray[i].position;
		}

		//calculate new directions
		BoidBehaviorJob behaviorJob = new BoidBehaviorJob()
		{
			Positions = _positions,
			Velocities = _velocities,
			NewVelocities = _newVelocities,
			SeparationWeight = _separationWeight,
			AlignmentWeight = _alignmentWeight,
			CohesionWeight = _cohesionWeight,
			NeighborDistance = _neighborDistance,
			MoveSpeed = _moveSpeed,
			PlayerPos = _playerPosition.position,
			PlayerEffectWeight = _playerEffectWeight,
			AvoidPlayer = _shouldAvoidPlayer,
			BoundsCenter = new float3(
				(boundsMin.x + boundsMax.x) / 2f,
				(boundsMin.y + boundsMax.y) / 2f,
				0f),
			CenterPullWeight = _centerPullWeight,
			MaxNeighbors = _maxNeighbors,
			LookAheadTime = _lookAhead,
			Randoms = _randoms

		};

		JobHandle behaviorHandle = behaviorJob.Schedule(_numberOfBoidsToSpawn, 64);

		//apply movement
		BoidMoveJob moveJob = new BoidMoveJob()
		{
			NewVelocities = _newVelocities,
			DeltaTime = Time.deltaTime,
			MinX = boundsMin.x,
			MaxX = boundsMax.x,
			MinY = boundsMin.y,
			MaxY = boundsMax.y
		};

		JobHandle moveHandle = moveJob.Schedule(_accessArray, behaviorHandle);

		moveHandle.Complete();

		// copy new velocities for the next frame
		_velocities.CopyFrom(_newVelocities);
	}

	void OnDestroy()
	{
		if (_accessArray.isCreated) _accessArray.Dispose();
		if (_positions.IsCreated) _positions.Dispose();
		if (_velocities.IsCreated) _velocities.Dispose();
		if (_newVelocities.IsCreated) _newVelocities.Dispose();
		if (_randoms.IsCreated) _randoms.Dispose();
	}
}
[BurstCompile]
public struct BoidBehaviorJob : IJobParallelFor
{
	[ReadOnly] public NativeArray<float3> Positions;
	[ReadOnly] public NativeArray<float3> Velocities;
	[ReadOnly] public float3 PlayerPos;
	[ReadOnly] public float PlayerEffectWeight;
	[ReadOnly] public bool AvoidPlayer;
	[ReadOnly] public float3 BoundsCenter;
	[ReadOnly] public float CenterPullWeight;
	[ReadOnly] public int MaxNeighbors;
	[ReadOnly] public float LookAheadTime;

	[NativeDisableParallelForRestriction] // Permite leitura e escrita no Randoms
	public NativeArray<Unity.Mathematics.Random> Randoms;

	public NativeArray<float3> NewVelocities;

	public float SeparationWeight;
	public float AlignmentWeight;
	public float CohesionWeight;
	public float NeighborDistance;
	public float MoveSpeed;

	public void Execute(int index)
	{
		float3 currentPos = Positions[index];
		float3 currentVel = Velocities[index];

		float3 separation = float3.zero;
		float3 alignment = float3.zero;
		float3 cohesion = float3.zero;

		Unity.Mathematics.Random rand = Randoms[index];

		int neighborCount = 0;

		float3 playerDir = PlayerPos - currentPos;
		if (math.lengthsq(playerDir) > 0)
			playerDir = math.normalize(playerDir);
		else
			playerDir = float3.zero;

		if (AvoidPlayer) playerDir = -playerDir;

		float NeighborDistanceSquared = NeighborDistance * NeighborDistance;
		float NeighborDistanceSquaredOffset = NeighborDistanceSquared * 0.25f;

		// Normaliza velocidade atual para previsão
		float3 normCurrentVel = math.lengthsq(currentVel) > 0 ? math.normalize(currentVel) : float3.zero;

		float3 predictedSelfPos = currentPos + normCurrentVel * MoveSpeed * LookAheadTime;

		for (int i = 0; i < Positions.Length; i++)
		{
			if (i == index) continue;

			float3 otherPos = Positions[i];
			float3 offset = currentPos - otherPos;
			float distSqr = math.lengthsq(offset);

			if (distSqr < NeighborDistanceSquared && distSqr > 0)
			{
				float invDist = math.rsqrt(distSqr);
				separation += offset * invDist;
				alignment += Velocities[i];
				cohesion += otherPos;
				neighborCount++;

				// Previsão de posição do vizinho
				float3 normOtherVel = math.lengthsq(Velocities[i]) > 0 ? math.normalize(Velocities[i]) : float3.zero;
				float3 predictedOtherPos = otherPos + normOtherVel * MoveSpeed * LookAheadTime;

				float3 predictedOffset = predictedSelfPos - predictedOtherPos;
				float predictedDistSqr = math.lengthsq(predictedOffset);

				if (predictedDistSqr < NeighborDistanceSquaredOffset && predictedDistSqr > 0)
				{
					float invPredDist = math.rsqrt(predictedDistSqr);
					separation += predictedOffset * invPredDist * SeparationWeight; // Usa peso de separação
				}

				if (neighborCount >= MaxNeighbors) break;
			}
		}

		if (neighborCount > 0)
		{
			separation = math.normalize(separation / neighborCount);
			alignment = math.normalize(alignment / neighborCount);
			cohesion = math.normalize((cohesion / neighborCount) - currentPos);
		}

		float3 toCenter = BoundsCenter - currentPos;
		toCenter = math.normalize(toCenter);

		float3 finalVel = currentVel;
		finalVel += separation * SeparationWeight;
		finalVel += alignment * AlignmentWeight;
		finalVel += cohesion * CohesionWeight;
		finalVel += playerDir * PlayerEffectWeight;
		finalVel += toCenter * CenterPullWeight;

		if (math.lengthsq(finalVel) > 0)
		{
			finalVel = math.normalize(finalVel) * MoveSpeed;
		}
		else
		{
			float2 dir = rand.NextFloat2(-1f, 1f);
			dir = math.normalize(dir);
			finalVel = new float3(dir.x, dir.y, 0) * MoveSpeed;
		}

		NewVelocities[index] = finalVel;

	}
}

[BurstCompile]
public struct BoidMoveJob : IJobParallelForTransform
{
	public NativeArray<float3> NewVelocities;
	[ReadOnly] public float DeltaTime;
	[ReadOnly] public float MinX;
	[ReadOnly] public float MaxX;
	[ReadOnly] public float MinY;
	[ReadOnly] public float MaxY;

	public void Execute(int index, TransformAccess transform)
	{
		float margin = 0.2f;
		float safeMinX = MinX + margin;
		float safeMaxX = MaxX - margin;
		float safeMinY = MinY + margin;
		float safeMaxY = MaxY - margin;

		float3 velocity = NewVelocities[index];
		float3 newPos = (float3)transform.position + velocity * DeltaTime;

		// Rebote nas X
		if (newPos.x < safeMinX)
		{
			newPos.x = safeMinX;
			velocity.x = math.abs(velocity.x); // força direita
		}
		else if (newPos.x > safeMaxX)
		{
			newPos.x = safeMaxX;
			velocity.x = -math.abs(velocity.x); // força esquerda
		}

		// Rebote nas Y
		if (newPos.y < safeMinY)
		{
			newPos.y = safeMinY;
			velocity.y = math.abs(velocity.y); // força cima
		}
		else if (newPos.y > safeMaxY)
		{
			newPos.y = safeMaxY;
			velocity.y = -math.abs(velocity.y); // força baixo
		}

		// Posiciona
		transform.position = newPos;

		const float minSpeedSqr = 0.0001f; // ou outro valor pequeno adequado

		if (math.lengthsq(velocity) > minSpeedSqr)
		{
			float angleRad = math.atan2(velocity.y, velocity.x);
			angleRad += math.radians(180f);
			transform.rotation = quaternion.AxisAngle(new float3(0, 0, 1), angleRad);
		}

		// Salva de volta — GARANTE consistência para o próximo frame
		NewVelocities[index] = velocity;
	}
}
