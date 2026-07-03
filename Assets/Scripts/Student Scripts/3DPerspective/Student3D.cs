using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Student3D : Student
{
    [SerializeField] private Animator animator;

    private bool _resting = false;
    private float _workingMultiplier = 1.0f;
    private bool _distracted = false;
    private bool _burnedOut = false;
    private bool _failed = false;
    private bool _victory = false;
    private bool _isDragged = false;

    public bool Resting { get => _resting; set {_resting = value; animator?.SetBool("Resting", value); } }
    public float WorkingMultiplier { get => _workingMultiplier; set {_workingMultiplier = value; animator?.SetFloat("WorkingMultiplier", value); } }
    public bool Distracted { get => _distracted; set {_distracted = value; animator?.SetBool("Distracted", value); } }
    public bool BurnedOut { get => _burnedOut; set {_burnedOut = value; animator?.SetBool("BurnedOut", value); } }
    public bool Failed { get => _failed; set {_failed = value; animator?.SetBool("Failed", value); } }
    public bool Victory { get => _victory; set {_victory = value; animator?.SetBool("Victory", value); } }
    public bool IsDragged { get => _isDragged; set {_isDragged = value; animator?.SetBool("IsDragged", value); } }

    private Plane dragPlane;
    private Vector3 dragOffset;
    private Camera mainCamera;
    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;
        dragPlane = new Plane(Vector3.up, Vector3.zero);
        Working();
    }
    
    public void FailPartial() {
        animator.SetTrigger("FailPartial");
    }

    public void PartialPassed() {
        animator.SetTrigger("PartialPassed");
    }

    public void Grounded() {
        //animator.ResetTrigger("Grounded", true);
        animator.SetTrigger("Grounded");
    }

    private void Working() {
        Resting = false;
        WorkingMultiplier = 1.0f;
        Distracted = false;
        IsDragged = false;
        BurnedOut = false;
    }

    public override void ChangeState(StudentState newState){
        StudentState currState = currentState;
        base.ChangeState(newState);
        switch (newState)
        {
            case StudentState.Working:
                Working();
                if (currState == StudentState.Distracted) {
                    Grounded();
                }
                break;
            case StudentState.Flow:
                WorkingMultiplier = 1.5f;
                break;
            case StudentState.Burnout:
                Working();
                BurnedOut = true;
                break;
            case StudentState.Resting:
                Working();
                Resting = true;
                break;
            case StudentState.DroppedOut:
                Working();
                Failed = true;
                break;
            case StudentState.Distracted:
                Working();
                Distracted = true;
                break;
            case StudentState.Finished:
                Working();
                PartialPassed();
                break;
            case StudentState.Graduated:
                Working();
                Victory = true;
                break;
            default:
                Working();
                break;
        }
   
    }

    public override void OnBeginDrag(PointerEventData eventData)  { 
        Debug.Log("OnBeginDrag");
        originalPosition = transform.position; 
        
        // 1. Creamos un piso matemático invisible exactamente a la altura del alumno
        dragPlane = new Plane(Vector3.up, transform.position);

        // 2. Calculamos de dónde lo agarramos para que no salte al centro del mouse
        Ray camRay = Camera.main.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(camRay, out float distance))
        {
            dragOffset = transform.position - camRay.GetPoint(distance);
        }
    }
    
    public override void OnDrag(PointerEventData eventData) {
        Ray camRay = Camera.main.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(camRay, out float distance))
        {
            transform.position = camRay.GetPoint(distance) + dragOffset;
        }
        
    }

    public override void OnEndDrag(PointerEventData eventData) {
        bool dragExitoso = false;
        float snapRadius = 3f; // Quizás en 3D necesites ajustar este número un poco
        Seat[] todasLasSillas = FindObjectsByType<Seat>(FindObjectsSortMode.None);        
        Seat sillaMasCercana = null;
        float distanciaMinima = float.MaxValue;

        foreach (Seat silla in todasLasSillas)
        {
            // OJO: Usamos Vector3.Distance para 3D
            float distancia = Vector3.Distance(transform.position, silla.transform.position);
            if (distancia < distanciaMinima && distancia <= snapRadius)
            {
                distanciaMinima = distancia;
                sillaMasCercana = silla;
            }
        }

        // ... (El resto de tu lógica de intercambiar sillas se queda exactamente igual)
        if (sillaMasCercana != null)
        {
            if (sillaMasCercana.currentStudent != null && sillaMasCercana.currentStudent != this)
            {
                Seat miSillaVieja = this.currentSeat;
                Seat suSillaVieja = sillaMasCercana;
                Student elOtroAlumno = sillaMasCercana.currentStudent;

                if (miSillaVieja != null)
                {
                    suSillaVieja.AssignStudent(this);
                    miSillaVieja.AssignStudent(elOtroAlumno);
                    dragExitoso = true;
                }
            }
            else if (sillaMasCercana.currentStudent == null)
            {
                Seat miSillaVieja = this.currentSeat;
                if (miSillaVieja != null) miSillaVieja.currentStudent = null; 

                sillaMasCercana.AssignStudent(this); 
                dragExitoso = true;
            }
        }

        if (dragExitoso == false)
        {
            if (currentSeat != null) transform.position = currentSeat.transform.position;
            else transform.position = originalPosition;
        }
    }
}
