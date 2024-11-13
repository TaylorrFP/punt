using Sandbox;
using System.Numerics;
using System;
using Sandbox.Utility;
using static Sandbox.VertexLayout;

public sealed class ShakeEffect : Component
{

	// Amplitude of position shake
	[Property] public float positionShakeAmount { get; set; } = 0.1f;
	// Frequency of position shake
	[Property] public float positionShakeFrequency { get; set; } = 10.0f;

	// Amplitude of rotation shake
	[Property] public float rotationShakeAmount { get; set; } = 5.0f;
	// Frequency of rotation shake
	[Property] public float rotationShakeFrequency { get; set; } = 10.0f;

	[Property] public float Strength { get; set; } = 1f;

	private Vector3 initialPosition;
	private Rotation initialRotation;
	private float positionTime;
	private float rotationTime;

	protected override void OnStart()
	{
		initialPosition = GameObject.LocalPosition;
		initialRotation = GameObject.LocalRotation;
	}

	protected override void OnUpdate()
	{

		// Increment time for different frequencies
		positionTime += Time.Delta * (1/Scene.TimeScale) * positionShakeFrequency;
		rotationTime += Time.Delta * (1 / Scene.TimeScale) * rotationShakeFrequency;

		// Calculate new position offset using Perlin noise for smooth randomness
		Vector3 positionOffset = new Vector3(
			(Noise.Perlin( positionTime, 0 ) - 0.5f) * 2 * positionShakeAmount,
			(Noise.Perlin( 0, positionTime ) - 0.5f) * 2 * positionShakeAmount,
			(Noise.Perlin( positionTime, positionTime ) - 0.5f) * 2 * positionShakeAmount
		);

		// Calculate new rotation offset in degrees
		float pitch = (Noise.Perlin( rotationTime, 0 ) - 0.5f) * 2 * rotationShakeAmount;
		float yaw = (Noise.Perlin( 0, rotationTime ) - 0.5f) * 2 * rotationShakeAmount;
		float roll = (Noise.Perlin( rotationTime, rotationTime ) - 0.5f) * 2 * rotationShakeAmount;
		Rotation rotationOffset = Rotation.From( pitch, yaw, roll );

		// Apply the new position and rotation offsets
		GameObject.LocalPosition = initialPosition + positionOffset*Strength;
		GameObject.LocalRotation = Rotation.Lerp( Rotation.Identity, rotationOffset, Strength );//rotationOffset;

	}
}
