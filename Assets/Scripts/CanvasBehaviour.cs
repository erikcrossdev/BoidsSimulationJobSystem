using TMPro;
using UnityEngine;

public class CanvasBehaviour : MonoBehaviour
{
	[SerializeField] BoidsSpawnerAndBehaviour _boids;
	[SerializeField] UnityEngine.UI.Slider _moveSpeedSlider;
	[SerializeField] UnityEngine.UI.Slider _centerPullWeightSlider;
	[SerializeField] UnityEngine.UI.Slider _neighborDistanceSlider;
	[SerializeField] UnityEngine.UI.Slider _separationWeightSlider;
	[SerializeField] UnityEngine.UI.Slider _alignmentWeightSlider;
	[SerializeField] UnityEngine.UI.Slider _cohesionWeightSlider;
	[SerializeField] UnityEngine.UI.Slider _playerEffectWeightSlider;
	[SerializeField] UnityEngine.UI.Toggle _shouldAvoidPlayerToggle;
	[SerializeField] UnityEngine.UI.Slider _lookAhead;
	[SerializeField] UnityEngine.UI.Button ToggleButton;
	[SerializeField] TextMeshProUGUI ButtonLabel;
	[SerializeField] GameObject Root;

	private bool RootState = true;
	[SerializeField] private TextMeshProUGUI fpsText;

	private float deltaTime = 0.0f;
	void Start()
    {
		_moveSpeedSlider.onValueChanged.AddListener(_boids.OnChangeMoveSpeed);
		_centerPullWeightSlider.onValueChanged.AddListener(_boids.OnChangeCenterPullWeight);
		_neighborDistanceSlider.onValueChanged.AddListener(_boids.OnChangeNeighborDistance);
		_separationWeightSlider.onValueChanged.AddListener(_boids.OnChangeSeparationWeight);
		_alignmentWeightSlider.onValueChanged.AddListener(_boids.OnChangeAlignmentWeight);
		_cohesionWeightSlider.onValueChanged.AddListener(_boids.OnChangeCohesionWeight);
		_playerEffectWeightSlider.onValueChanged.AddListener(_boids.OnChangePlayerEffectWeight);
		_shouldAvoidPlayerToggle.onValueChanged.AddListener(_boids.OnChangeShouldAvoidPlayer);
		_lookAhead.onValueChanged.AddListener(_boids.OnChangeLookAhead);

		ToggleButton.onClick.AddListener(OnToggle);

		_moveSpeedSlider.value = _boids.MoveSpeed;
		_centerPullWeightSlider.value = _boids.CenterPullWeight;
		_neighborDistanceSlider.value = _boids.NeighborDistance;
		_separationWeightSlider.value = _boids.SeparationWeight;
		_alignmentWeightSlider.value = _boids.AlignmentWeight;
		_cohesionWeightSlider.value = _boids.CohesionWeight;
		_playerEffectWeightSlider.value = _boids.PlayerEffectWeight;
		_shouldAvoidPlayerToggle.isOn = _boids.ShouldAvoidPlayer;
		_lookAhead.value = _boids.LookAhead;

		OnToggle();
	}

	public void OnToggle() {
		RootState = !RootState;
		Root.SetActive(RootState);
		ButtonLabel.text = RootState == true ? "Hide UI" : "Simulate";
	}
	

	void Update()
	{
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
		float fps = 1.0f / deltaTime;
		fpsText.text = Mathf.Ceil(fps).ToString();
	}

}
