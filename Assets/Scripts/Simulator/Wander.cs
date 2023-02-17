using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Simulator
{
	/// <summary>
	/// Creates wandering behaviour for a CharacterController.
	/// </summary>
	[RequireComponent(typeof(CharacterController))]
	public class Wander : MonoBehaviour
	{
		[SerializeField] private float speed = 1;
		[SerializeField] private float acceleration = 0.001f;
		public float directionChangeInterval = 1;
		public float maxHeadingChange = 30;

		public float maxDistanceFactor = 10;

		CharacterController controller;
		float heading;
		Vector3 targetRotation;
		
		[SerializeField] private int Seed = 42;

		private void Start ()
		{
			Random.InitState(Seed);
			controller = GetComponent<CharacterController>();

			// Set random initial rotation
			if (Seed != 0)
			{
				heading = Random.Range(0, 360);
				transform.eulerAngles = new Vector3(0, heading, 0);
			}

			StartCoroutine(NewHeading());
		}

		private void FixedUpdate ()
		{
			Vector3 vectorToCenter = Vector3.zero - transform.position;
			float maxRadiansDelta = Mathf.Exp(vectorToCenter.magnitude)/ (1000 * maxDistanceFactor);
			Vector3 newDirection = Vector3.RotateTowards(transform.forward, vectorToCenter, maxRadiansDelta, 0f);
			newDirection.y = 0;
			transform.rotation = Quaternion.LookRotation(newDirection);
		
			transform.eulerAngles = Vector3.Slerp(transform.eulerAngles, targetRotation, Time.deltaTime * directionChangeInterval);
			Vector3 forward = transform.TransformDirection(Vector3.forward);
			controller.SimpleMove(forward * speed);

			speed += acceleration;
		}

		/// <summary>
		/// Repeatedly calculates a new direction to move towards.
		/// Use this instead of MonoBehaviour.InvokeRepeating so that the interval can be changed at runtime.
		/// </summary>
		private IEnumerator NewHeading ()
		{
			while (true) {
				NewHeadingRoutine();
				yield return new WaitForSeconds(directionChangeInterval);
			}
		}

		/// <summary>
		/// Calculates a new direction to move towards.
		/// </summary>
		private void NewHeadingRoutine ()
		{
			var floor = transform.eulerAngles.y - maxHeadingChange;
			var ceil  = transform.eulerAngles.y + maxHeadingChange;
			heading = Random.Range(floor, ceil);
			targetRotation = new Vector3(0, heading, 0);
		}
	}
}