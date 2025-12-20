using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class Config {
	public static class Player {
		public static class Behaviour {
			public static float SitDistance = 3;
		}

		public static class Controller {
			public static float Speed = 3;
			public static Vector2 FirstPersonPitchLimits = new(-60, 80);
			public static Vector2 ThirdPersonPitchLimits = new(-10, 85);
			public static float Sensitivity = .05f;
			public static float OperatingSensitivity = .15f;
		}

		public static class Camera {
			public static float StickAngle					= 6;
			public static float stickCameraOffset			= .1f;
			public static float L							= .13f;
			public static float R							= 2.56f;
			public static float S							= .92f;
			public static float transitionScrollStrength	= 1f;
			public static float transitionFovSmoothing		= 1f;
			public static float transitionFovEffect			= 10f;
			public static float transitionResidual			= 5f;
			public static float tpDistMin					= 2f;
			public static float tpDistMax					= 5f;
			public static float tpOuterFovExtra				= 10f;
			public static float tpFovExtraPow				= 5f;
			public static float tpScrollSensitivity			= .007f;
			public static float tpDistSmooth				= 5f;
			public static float tpNeckRotationCoef			= .1f;
			public static float tpCollisionOutset			= .5f;
			public static float hideFaceTpDist				= .7f;
			public static Vector4	tpCloseFalloff			= new(.98f, .24f, 1.61f, 2.18f);
			public static float zoomSens					= .0005f;
			public static float zoomCurveSlope				= 5f;
			public static float maxZoom						= 60f;
			public static float zoomSmooth					= 10f;
			public static float firstPersonYawFreedom		= 30f;
			public static float thirdPersonYawFreedom		= 170f;
			public static float movingYawMatchSmooth		= 7f;
			public static float bodyYawSmooth				= 10f;
		}

		public static class Feet {
			public static float maxDist				= .5f;
			public static float stepForward			= .5f;
			public static float maxStepForward		= 1.5f;
			public static float distanceInfluence	= .5f;
			public static float stepUp				= 1f;
			public static float angleUp				= .5f;
			public static float hipWidth			= 1;
			public static float animSpeed			= 5f;
			public static float animSSin			= 1.5f;
			public static float animSSout			= 4;
			public static float velSmoothing		= 10f;
		}

		public static class Beak {
			public static float TopOpenAngle = 9;
			public static float BottomOpenAngle = 28;
			public static float OpenSmoothness = 5;
		}
	}
}