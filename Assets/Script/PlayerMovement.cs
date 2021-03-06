using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 5.0f;
	[SerializeField] private float sinkSpeed = 1.0f;
	[SerializeField] private float frontCheckDistance = 0.7f;
	[SerializeField] private LayerMask wallDetectionMask;
	[SerializeField] private LayerMask waterSurfaceDetectionMask;

	private Vector2 currentDirection = Vector2.zero;
	public Vector2Update OnMoveDirectionUpdated;
	public SimpleEvent OnStunned;
	public SimpleEvent OnStunRecover;
	public SimpleEvent OnBounce;

	//! Fields for stun
	private bool stunned;
	private float stunTimeElapsed;
	private float totalStunDuration;
	private Coroutine stunCoroutine;
	
    public void SetPlayerMoveDirection(Vector2 _targetPoint)
    {
		if (stunned)
		{
			Debug.Log("Player character is stunned, cannot set move location");
			return;
		}

        PlayerManagerScript.Instance.TogglePlayerMoving(true);
        PlayerManagerScript.Instance.SwitchDirectionEnergy();

        currentDirection = (_targetPoint - (Vector2)this.transform.position).normalized;
		RotateToDirection();
    }

	//! Apply stun duration. Player would be stunned as long as the stun time elapsed is less than the total stun duration.
	//! Calling this method while the player is stunned would not refresh the elapsed duration
	public void ApplyStun(float _duration)
	{
        PlayerManagerScript.Instance.TogglePlayerMoving(false);
        PlayerManagerScript.Instance.StunnedEnergyLost();

        if (totalStunDuration < _duration)
		{
			totalStunDuration = _duration;
		}

		if (stunCoroutine == null)
		{
			stunCoroutine = StartCoroutine(ProcessStun());
		}

		OnStunned?.Invoke();
		
		StopPlayer();
	}

	public void StopPlayer()
	{
		currentDirection = Vector2.zero;
	}

	public void WallCollision(Collision2D collision)
	{
		//! Don't react from colliding with ball. Seal would still bounce off when they hit water
		if (!collision.transform.CompareTag("Ball") && !collision.transform.CompareTag("Item"))
		{
			Bounce(collision.contacts[0].normal);
		}

        // Player bounce off the bomb.
        if (collision.gameObject.name.Contains("Bomb"))
        {
            Bounce(collision.contacts[0].normal);
        }
	}

    private void FixedUpdate()
    {
		if (stunned)
		{
			PlayerSink();
		}
		else
		{
			PlayerMove();
		}
    }

	private void PlayerMove()
	{
		this.transform.Translate(currentDirection * movementSpeed * Time.deltaTime, Space.World);
	}

	private void PlayerSink()
	{
		Debug.DrawRay(this.transform.position, Vector2.down * 0.6f, Color.white);
		var hit = Physics2D.Raycast(this.transform.position, Vector2.down, 0.6f, wallDetectionMask);
		if (!hit)
		{
			this.transform.Translate(Vector3.down * sinkSpeed * Time.deltaTime, Space.World);
		}
	}

	private bool CheckForWall()
	{
		RaycastHit2D hit = Physics2D.Raycast(this.transform.position, currentDirection, frontCheckDistance, wallDetectionMask.value);
		if (hit)
		{
			Debug.Log("Hit wall");
			Debug.DrawRay(this.transform.position, currentDirection * 10f, Color.red, 3.0f);

			var r = currentDirection - (2 * (Vector2.Dot(currentDirection, hit.normal.normalized))) * hit.normal.normalized;

			Debug.DrawRay(hit.point, r * 10f, Color.white, 3f);

			currentDirection = r.normalized;

			RotateToDirection();
		}

		return hit;
	}

	private void RotateToDirection()
	{
		var angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;

		this.transform.rotation = Quaternion.Euler(new Vector3(0,0,angle - 90));

		OnMoveDirectionUpdated?.Invoke(currentDirection);
	}

	private void Bounce(Vector2 collisionNormal)
	{
		currentDirection = Vector2.Reflect(currentDirection, collisionNormal);

		RotateToDirection();

		OnBounce?.Invoke();
	}
	private IEnumerator ProcessStun()
	{
		do
		{
			stunned = stunTimeElapsed < totalStunDuration;
			yield return null;
			stunTimeElapsed += Time.deltaTime;
		} while (stunned);

		stunTimeElapsed = 0;
		totalStunDuration = 0;

		stunCoroutine = null;

		OnStunRecover?.Invoke();
	}
}

[System.Serializable]
public class Vector2Update : UnityEvent<Vector2>
{

}

[System.Serializable]
public class SimpleEvent : UnityEvent
{

}
