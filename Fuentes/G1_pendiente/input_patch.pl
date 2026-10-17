# Agrega Tool6 (hacha) y Tool7 (pico) con las teclas 6 y 7 al archivo de controles (G1).
local $/; my $s = <STDIN>;
my $act = <<'J';
                {
                    "name": "Tool6",
                    "type": "Button",
                    "id": "43a69ba4-6267-4817-bf00-7f6fb3a71478",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Tool7",
                    "type": "Button",
                    "id": "bde58fd6-3330-4ada-aeb5-3ec9f855191f",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
J
$s =~ s{(                \{\n                    "name": "Tool5",\n(?:.*\n){6}                \},\n)}{$1$act} or die "accion";
my $bind = <<'J';
                {
                    "name": "",
                    "id": "1898ba64-870d-49de-8baa-f2c15fac5c7d",
                    "path": "<Keyboard>/6",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Tool6",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "b2d15476-410c-4e40-8a39-e80db4c1839e",
                    "path": "<Keyboard>/7",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Tool7",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
J
$s =~ s{(                    "action": "Tool5",\n                    "isComposite": false,\n                    "isPartOfComposite": false\n                \},\n)}{$1$bind} or die "tecla";
print $s;
