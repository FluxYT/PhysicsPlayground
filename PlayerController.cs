using Godot;

namespace PhysicsPlayground;

public partial class PlayerController : CharacterBody3D
{
    [Export] private float Speed { get; set; } = 4;
    [Export] private float MouseSensitivity { get; set; } = 0.002f;

    [Export] private Camera3D Camera { get; set; }
    [Export] private TextureRect Crosshair { get; set; }
    [Export] private Node3D InteractionPos { get; set; }
    [Export] private RayCast3D InteractionRay { get; set; }
    [Export] private Generic6DofJoint3D InteractionJoint { get; set; }
    [Export] private StaticBody3D JointA { get; set; }
    
    private RigidBody3D GrabbedObject { get; set; }
    private bool LockRotation { get; set; } = false;

    public override void _Ready()
    {
        Input.SetMouseMode(Input.MouseModeEnum.Captured);
    }

    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.Q))
        {
            GetTree().Quit();
        }

        Crosshair.Visible = false;
        
        if (InteractionRay.GetCollider() is not RigidBody3D) return;
        
        var collider = (RigidBody3D)InteractionRay.GetCollider();
        if (!collider.IsInGroup("Interactable")) return;
            
        Crosshair.Visible = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        GetInput();
        MoveAndSlide();

        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (collision.GetCollider() is RigidBody3D collisionBody)
            {
                collisionBody.ApplyCentralImpulse(-collision.GetNormal() * 10f);
            }
        }

        if (GrabbedObject != null)
        {
            var clampedMass = Mathf.Clamp(GrabbedObject.Mass, 0f, 50f);
            var grabForce = Mathf.Lerp(5f, 0f, Mathf.Remap(clampedMass, 0f, 50f, 0f, 1f));
            GrabbedObject.SetLinearVelocity((InteractionPos.GlobalPosition - GrabbedObject.GlobalPosition) * grabForce);
        }
        
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion eventMouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            if (!LockRotation)
            {
                RotateY(-eventMouseMotion.Relative.X * MouseSensitivity);
                Camera.RotateX(-eventMouseMotion.Relative.Y * MouseSensitivity);
                Camera.Rotation = new Vector3(Mathf.Clamp(Camera.Rotation.X, -Mathf.DegToRad(70f), Mathf.DegToRad(70f)), Camera.Rotation.Y, Camera.Rotation.Z);
            }
            
            if (Input.IsActionPressed("right_click"))
            {
                if (GrabbedObject != null)
                {
                    LockRotation = true;
                    var clampedMass = Mathf.Clamp(GrabbedObject.Mass, 0f, 50f);
                    var rotationForce = Mathf.Lerp(0.1f, 0f, Mathf.Remap(clampedMass, 0f, 50f, 0f, 1f));
                    JointA.Rotate(Camera.GlobalBasis.X, Mathf.DegToRad(eventMouseMotion.Relative.Y * rotationForce));
                    JointA.Rotate(Camera.GlobalBasis.Y, Mathf.DegToRad(eventMouseMotion.Relative.X * rotationForce));
                }
            }
        }
        
        if (Input.IsActionJustReleased("right_click"))
        {
            LockRotation = false;
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            Input.SetMouseMode(Input.MouseModeEnum.Visible);
        }

        if (@event.IsActionPressed("click"))
        {
            if (Input.MouseMode != Input.MouseModeEnum.Captured)
            {
                Input.SetMouseMode(Input.MouseModeEnum.Captured);
            }

            if (GrabbedObject != null)
            {
                var thrownObject = GrabbedObject;
                GrabbedObject = null;
                InteractionJoint.SetNodeB((InteractionJoint.GetPath()));
                var clampedMass = Mathf.Clamp(thrownObject.Mass, 0f, 50f);
                var throwForce = Mathf.Lerp(10f, 50f, Mathf.Remap(clampedMass, 0f, 50f, 0f, 1f));
                thrownObject.ApplyCentralImpulse(-Camera.GlobalBasis.Z * throwForce);
            }
        }
        
        if (@event.IsActionPressed("interact"))
        {
            GD.Print("Interact");
            if (GrabbedObject == null)
            {
                TryGrabObject();
            }
            else
            {
                GrabbedObject = null;
                InteractionJoint.SetNodeB((InteractionJoint.GetPath()));
            }
        }
    }

    private void GetInput()
    {
        var input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        var movementDir = Transform.Basis * new Vector3(input.X, 0, input.Y);
        SetVelocity(new Vector3(movementDir.X * Speed, Velocity.Y, movementDir.Z * Speed));
    }

    private void TryGrabObject()
    {
        if (InteractionRay.GetCollider() is not RigidBody3D) return;
        
        var collider = (RigidBody3D)InteractionRay.GetCollider();
        if (!collider.IsInGroup("Interactable")) return;

        GrabbedObject = collider;
        InteractionJoint.SetNodeB((GrabbedObject.GetPath()));
    }
}