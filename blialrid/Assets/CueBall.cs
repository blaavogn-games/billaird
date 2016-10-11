using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CueBall : MonoBehaviour
{
    public enum BallType { cue, blue, red, black };
    public enum BallState { still, moving, charging, shooting, pocketed, free, transToFree, pocketing };

    public BallType Type;
    public AudioClip collideSound, cueSound, holeSound;
    private AudioSource _audioSource;
    private ScoreBoard _scoreBoard;
    private BallState _state = BallState.still;
    private Transform _shadowBall;
    private Transform _stick;
    private Transform _target;
    private HashSet<int> _colSet = new HashSet<int>();
    private List<CueBall> _balls = new List<CueBall>();

    private float _charge = 0.0f, _shotCharge = 0.0f, _transTime = 0.0f, _pocketingTime = 3.2f;
    private bool _physicsStep = false;

    void Start()
    {
        _shadowBall = GameObject.FindGameObjectWithTag("Finish").transform;
        _stick = GameObject.FindGameObjectWithTag("Respawn").transform;
        _scoreBoard = GameObject.FindGameObjectWithTag("ScoreBoard").GetComponent<ScoreBoard>();
        _audioSource = GetComponent<AudioSource>();
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("Player"))
        {
            _balls.Add(g.GetComponent<CueBall>());
        }
    }
    
    private void UpdateCue()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 delta = mousePos - (Vector2)transform.position;
        switch (_state)
        {
            case BallState.still:
                RotateStick(delta);

                if (Input.GetMouseButtonDown(0))
                {
                    _state = BallState.charging;
                }
                break;
            case BallState.charging:
                RotateStick(delta);

                _charge += Time.deltaTime * 1.8f;
                if (_charge > 4)
                {
                    _charge = 4;
                }

                if (Input.GetMouseButtonUp(0))
                {
                    _shotCharge = _charge;
                    _state = BallState.shooting;
                }
                break;
            case BallState.shooting:
                RotateStick(delta);

                _charge -= Time.deltaTime * 14f * _shotCharge;
                if (_charge < -0.53f)
                {
                    _audioSource.volume = Mathf.Max(_shotCharge / 2, 0.2f);
                    _audioSource.PlayOneShot(cueSound);
                    GetComponent<Rigidbody2D>().AddForce(delta * 50 * _shotCharge);
                    _stick.position = new Vector2(1000,1000);
                    _shadowBall.position = new Vector2(100000,100000);
                    _state = BallState.moving;
                    _charge = 0.0f;
                    _shotCharge = 0.0f;
                    _physicsStep = false;
                }
                break;
            case BallState.moving:
                if (_physicsStep && !movement())
                {
                    _state = BallState.still;
                }
                break;
            case BallState.transToFree:
                _transTime -= Time.deltaTime;
                if (_physicsStep && !movement() && _transTime < 0.0f)
                {
                    _state = BallState.free;
                }
                break;
            case BallState.free:
                foreach (CueBall b in _balls)
                {
                    b.GetComponent<Rigidbody2D>().isKinematic = true;
                }
                transform.position = mousePos;
                if (Input.GetMouseButtonDown(0))
                {
                    foreach (CueBall b in _balls)
                    {
                        b.GetComponent<Rigidbody2D>().isKinematic = false;
                    }
                    _state = BallState.still;
                }
                break;
            case BallState.pocketing:
                Pocketing();
                break;
        }
    }

    private void UpdateNormal()
    {
        switch (_state)
        {
            case BallState.pocketing:
                Pocketing();
                break;
        }
    }

    private void Pocketing()
    {
        transform.position = Vector2.MoveTowards(transform.position, _target.position, 4f * Time.deltaTime);
        transform.localScale = new Vector2(_pocketingTime, _pocketingTime);
        _pocketingTime -= Time.deltaTime * 10;
        if(_pocketingTime < 0.3f)
        {
            if (Type == BallType.cue)
            {
                _pocketingTime = 3.2f;
                _state = BallState.free;
                transform.localScale = new Vector2(3.2f, 3.2f);
                GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            else
            {
                _state = BallState.pocketed;
                _scoreBoard.AddBall(this);
            }
        }
    }

    private bool movement()
    {
        foreach (CueBall b in _balls)
        {
            if (b.GetComponent<Rigidbody2D>().velocity.magnitude > 0.05f)
            {
                return true;
            }
        }
        return false;
    }

    private void RotateStick(Vector2 delta)
    {
        if(delta.magnitude < 0.1f)
        {
            return;
        }
        _stick.position = (Vector2)transform.position - (delta.normalized * (7 + _charge));

        float _r = Mathf.Atan2(transform.position.x - _stick.position.x, transform.position.y - _stick.position.y);
        float _d = (_r / Mathf.PI) * 180;

        _stick.rotation = Quaternion.Euler(0, 0, -1 * _d);

        RaycastHit2D hit = Physics2D.CircleCast((Vector2)transform.position, 0.46f, delta);
        
        if (hit)
        {
            _shadowBall.position = hit.centroid;
        }
        else
        {
            _shadowBall.position = new Vector2(-10000,-10000);
        }
    }

    void FixedUpdate()
    {
        _physicsStep = true;
    }

    void Update()
    {
        var rig = GetComponent<Rigidbody2D>();
        if(rig.velocity.magnitude < 1.5f)
        {
            rig.velocity *= 0.9f;
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        switch (Type)
        {
            case BallType.cue:
                UpdateCue();
                break;
            default:
                UpdateNormal();
                break;
        }
        _colSet.Clear();
    }

    public void AddToColSet(int id)
    {
        _colSet.Add(id);
    }

    public BallState GetState()
    {
        return _state;
    }

    public void ChangeBalls(BallType newType, BallState state, Sprite spr, int layer)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Type = newType;
        _state = state;
        sr.sprite = spr;
        gameObject.layer = layer;
    }
    
    void OnTriggerEnter2D(Collider2D col)
    {
        //Pocketing
        if (col.transform.tag == "GameController" && _state != BallState.free)
        {
            _state = BallState.pocketing;
            var rigid = GetComponent<Rigidbody2D>();
            rigid.velocity = Vector2.zero;
            _target = col.transform;
            rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            _audioSource.PlayOneShot(holeSound);
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.transform.tag == "Player")
        {
            int id = col.transform.GetInstanceID();
            if (!_colSet.Contains(id))
            {
                _colSet.Add(id);
                CueBall other = col.gameObject.GetComponent<CueBall>();
                other.AddToColSet(transform.GetInstanceID());
                var oldSprite = GetComponent<SpriteRenderer>().sprite;

                var thisVel = GetComponent<Rigidbody2D>().velocity.magnitude;
                var otherVel = other.GetComponent<Rigidbody2D>().velocity.magnitude;

                _audioSource.volume = Mathf.Max(Mathf.Sqrt(thisVel + otherVel) / 5,0.2f);
                Debug.Log(thisVel + otherVel);
                _audioSource.PlayOneShot(collideSound);

                BallType oldType = Type;
                BallState oldState = _state;
                int oldLayer = gameObject.layer;
                ChangeBalls(other.Type, other.GetState(), other.GetComponent<SpriteRenderer>().sprite, other.gameObject.layer);

                other.ChangeBalls(oldType, oldState, oldSprite, oldLayer);
            }
        }
        else
        {
            _audioSource.volume = 0.1f;
            _audioSource.PlayOneShot(cueSound);
        } 
    }
}
