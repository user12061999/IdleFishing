import os
import math

class ObjMesh:
    def __init__(self, name):
        self.name = name
        self.vertices = []
        self.faces = []

    def add_vertex(self, x, y, z):
        self.vertices.append((round(x, 4), round(y, 4), round(z, 4)))
        return len(self.vertices) # 1-based index

    def add_face(self, *indices):
        self.faces.append(indices)

    def add_box(self, center, size):
        cx, cy, cz = center
        sx, sy, sz = size[0] * 0.5, size[1] * 0.5, size[2] * 0.5
        v = [
            self.add_vertex(cx - sx, cy - sy, cz - sz), # 1
            self.add_vertex(cx + sx, cy - sy, cz - sz), # 2
            self.add_vertex(cx + sx, cy + sy, cz - sz), # 3
            self.add_vertex(cx - sx, cy + sy, cz - sz), # 4
            self.add_vertex(cx - sx, cy - sy, cz + sz), # 5
            self.add_vertex(cx + sx, cy - sy, cz + sz), # 6
            self.add_vertex(cx + sx, cy + sy, cz + sz), # 7
            self.add_vertex(cx - sx, cy + sy, cz + sz), # 8
        ]
        # Front, Back, Left, Right, Top, Bottom
        self.add_face(v[0], v[3], v[2], v[1])
        self.add_face(v[5], v[6], v[7], v[4])
        self.add_face(v[4], v[7], v[3], v[0])
        self.add_face(v[1], v[2], v[6], v[5])
        self.add_face(v[3], v[7], v[6], v[2])
        self.add_face(v[4], v[0], v[1], v[5])

    def add_cylinder(self, center, radius, height, segments=8):
        cx, cy, cz = center
        h_half = height * 0.5
        bottom_idx = []
        top_idx = []
        for i in range(segments):
            angle = 2 * math.pi * i / segments
            x = radius * math.cos(angle)
            z = radius * math.sin(angle)
            bottom_idx.append(self.add_vertex(cx + x, cy - h_half, cz + z))
            top_idx.append(self.add_vertex(cx + x, cy + h_half, cz + z))

        # Bottom and top centers
        b_c = self.add_vertex(cx, cy - h_half, cz)
        t_c = self.add_vertex(cx, cy + h_half, cz)

        for i in range(segments):
            nxt = (i + 1) % segments
            # Side face
            self.add_face(bottom_idx[i], top_idx[i], top_idx[nxt], bottom_idx[nxt])
            # Bottom cap
            self.add_face(b_c, bottom_idx[nxt], bottom_idx[i])
            # Top cap
            self.add_face(t_c, top_idx[i], top_idx[nxt])

    def add_cone(self, center, radius, height, segments=8):
        cx, cy, cz = center
        h_half = height * 0.5
        tip = self.add_vertex(cx, cy + h_half, cz)
        base_center = self.add_vertex(cx, cy - h_half, cz)
        base_idx = []
        for i in range(segments):
            angle = 2 * math.pi * i / segments
            x = radius * math.cos(angle)
            z = radius * math.sin(angle)
            base_idx.append(self.add_vertex(cx + x, cy - h_half, cz + z))

        for i in range(segments):
            nxt = (i + 1) % segments
            self.add_face(tip, base_idx[i], base_idx[nxt])
            self.add_face(base_center, base_idx[nxt], base_idx[i])

    def save_obj(self, filepath):
        with open(filepath, "w", encoding="utf-8") as f:
            f.write(f"# Wavefront OBJ low-poly generated: {self.name}\n")
            f.write(f"o {self.name}\n")
            for v in self.vertices:
                f.write(f"v {v[0]} {v[1]} {v[2]}\n")
            for face in self.faces:
                f.write("f " + " ".join(str(idx) for idx in face) + "\n")
        print(f"Generated {filepath} ({len(self.vertices)} verts, {len(self.faces)} faces)")

def build_fisherman():
    mesh = ObjMesh("Fisherman_Character")
    # Body / Life Vest
    mesh.add_box((0, 0.75, 0), (0.55, 0.65, 0.45))
    # Head
    mesh.add_cylinder((0, 1.25, 0), 0.22, 0.35, segments=8)
    # Bucket / Fishing Hat Brim & Crown
    mesh.add_cylinder((0, 1.40, 0), 0.42, 0.05, segments=8)
    mesh.add_cylinder((0, 1.50, 0), 0.26, 0.16, segments=8)
    # Legs (sitting or standing)
    mesh.add_box((-0.16, 0.25, 0), (0.2, 0.5, 0.25))
    mesh.add_box((0.16, 0.25, 0), (0.2, 0.5, 0.25))
    # Arms
    mesh.add_box((-0.35, 0.75, 0.12), (0.16, 0.5, 0.16))
    mesh.add_box((0.35, 0.75, 0.18), (0.16, 0.5, 0.16))
    # Fishing Rod
    mesh.add_cylinder((0.38, 1.15, 0.55), 0.03, 1.6, segments=6)
    return mesh

def build_wrangler():
    mesh = ObjMesh("Wrangler_Character")
    # Muscular Body with Apron
    mesh.add_box((0, 0.8, 0), (0.65, 0.75, 0.5))
    # Head with Beanie
    mesh.add_cylinder((0, 1.35, 0), 0.24, 0.38, segments=8)
    mesh.add_cylinder((0, 1.52, 0), 0.26, 0.12, segments=8)
    # Boots / Legs
    mesh.add_box((-0.2, 0.22, 0), (0.24, 0.45, 0.3))
    mesh.add_box((0.2, 0.22, 0), (0.24, 0.45, 0.3))
    # Right Arm with Big Wooden Fish Cudgel / Bat
    mesh.add_box((0.42, 0.82, 0.1), (0.2, 0.55, 0.2))
    mesh.add_cylinder((0.45, 0.95, 0.35), 0.08, 0.85, segments=6)
    # Left Arm
    mesh.add_box((-0.42, 0.82, 0), (0.2, 0.55, 0.2))
    return mesh

def build_world_portal():
    mesh = ObjMesh("WorldPortal_Gate")
    # Stone Base Plinth
    mesh.add_box((0, 0.2, 0), (3.6, 0.4, 1.4))
    # Left & Right Pillars
    mesh.add_cylinder((-1.4, 1.9, 0), 0.28, 3.0, segments=8)
    mesh.add_cylinder((1.4, 1.9, 0), 0.28, 3.0, segments=8)
    # Top Cross Arch Beam
    mesh.add_box((0, 3.45, 0), (3.4, 0.45, 0.55))
    # Portal Signpost / Crest
    mesh.add_box((0, 3.9, 0), (1.8, 0.55, 0.2))
    # Lighthouse Buoy / Lantern on top
    mesh.add_cone((-1.4, 3.75, 0), 0.35, 0.55, segments=8)
    mesh.add_cone((1.4, 3.75, 0), 0.35, 0.55, segments=8)
    # Shimmering Center Portal Archway (Inner Gateway)
    mesh.add_box((0, 1.8, 0), (2.1, 2.6, 0.12))
    return mesh

def build_arctic_tuna():
    mesh = ObjMesh("Fish_ArcticTuna")
    # Large streamlined torpedo body
    mesh.add_box((0, 0.3, 0), (0.55, 0.45, 1.5))
    # Pointed Head Nose
    mesh.add_cone((0, 0.3, 0.95), 0.26, 0.65, segments=8)
    # Dorsal Fin
    mesh.add_cone((0, 0.65, -0.1), 0.12, 0.45, segments=6)
    # Crescent Tail Fin
    mesh.add_box((0, 0.3, -0.95), (0.08, 0.65, 0.45))
    # Pectoral Fins
    mesh.add_box((-0.35, 0.25, 0.2), (0.35, 0.06, 0.22))
    mesh.add_box((0.35, 0.25, 0.2), (0.35, 0.06, 0.22))
    return mesh

def build_snow_crab():
    mesh = ObjMesh("Fish_SnowCrab")
    # Wide Carapace Shell
    mesh.add_cylinder((0, 0.2, 0), 0.55, 0.25, segments=8)
    # Front Big Claws (Left & Right)
    mesh.add_box((-0.55, 0.25, 0.55), (0.28, 0.18, 0.45))
    mesh.add_box((0.55, 0.25, 0.55), (0.28, 0.18, 0.45))
    mesh.add_cone((-0.55, 0.25, 0.85), 0.12, 0.3, segments=6)
    mesh.add_cone((0.55, 0.25, 0.85), 0.12, 0.3, segments=6)
    # 6 Walking Legs
    for side in (-1, 1):
        for z in (-0.25, 0.0, 0.25):
            mesh.add_box((side * 0.75, 0.12, z), (0.45, 0.08, 0.1))
    return mesh

def build_clownfish():
    mesh = ObjMesh("Fish_Clownfish")
    # Chubby rounded body
    mesh.add_box((0, 0.28, 0), (0.42, 0.5, 0.95))
    # Head
    mesh.add_cone((0, 0.28, 0.6), 0.24, 0.42, segments=8)
    # Wavy Tail Fin
    mesh.add_box((0, 0.28, -0.65), (0.06, 0.52, 0.38))
    # Flowing Dorsal Fin
    mesh.add_box((0, 0.62, -0.05), (0.06, 0.25, 0.55))
    # Pectoral Fins
    mesh.add_box((-0.26, 0.22, 0.1), (0.22, 0.05, 0.2))
    mesh.add_box((0.26, 0.22, 0.1), (0.22, 0.05, 0.2))
    return mesh

def build_pufferfish():
    mesh = ObjMesh("Fish_Pufferfish")
    # Spherical / Round Puffed Body
    mesh.add_cylinder((0, 0.35, 0), 0.45, 0.65, segments=8)
    mesh.add_cylinder((0, 0.35, 0), 0.55, 0.35, segments=8)
    # Tiny Fins & Tail
    mesh.add_box((0, 0.35, -0.48), (0.05, 0.3, 0.25))
    mesh.add_box((-0.38, 0.35, 0), (0.18, 0.05, 0.18))
    mesh.add_box((0.38, 0.35, 0), (0.18, 0.05, 0.18))
    # Spines around body
    mesh.add_cone((0, 0.78, 0), 0.1, 0.25, segments=6)
    mesh.add_cone((0, 0.72, 0.25), 0.08, 0.22, segments=6)
    mesh.add_cone((0, 0.72, -0.25), 0.08, 0.22, segments=6)
    mesh.add_cone((-0.48, 0.5, 0), 0.08, 0.22, segments=6)
    mesh.add_cone((0.48, 0.5, 0), 0.08, 0.22, segments=6)
    return mesh

def build_treasure_chest():
    mesh = ObjMesh("Sunken_Treasure_Chest")
    # 1. Main Chest Base
    mesh.add_box((0, 0.28, 0), (1.0, 0.55, 0.68))
    # 2. Arched Wooden Lid
    mesh.add_box((0, 0.62, 0), (0.98, 0.22, 0.66))
    mesh.add_box((0, 0.74, 0), (0.76, 0.12, 0.54))
    # 3. Reinforced Iron Bands (Left, Right, Center)
    mesh.add_box((-0.35, 0.45, 0), (0.09, 0.75, 0.71))
    mesh.add_box((0.35, 0.45, 0), (0.09, 0.75, 0.71))
    mesh.add_box((0.0, 0.45, 0), (0.09, 0.75, 0.71))
    # 4. Front Golden Lock Clasp
    mesh.add_box((0, 0.50, 0.36), (0.16, 0.18, 0.08))
    # 5. Sunken Barnacles / Sea Coral Accents
    mesh.add_cone((-0.42, 0.05, 0.32), 0.08, 0.14, segments=5)
    mesh.add_cone((0.42, 0.05, -0.32), 0.08, 0.14, segments=5)
    mesh.add_cone((0.25, 0.76, 0.18), 0.06, 0.12, segments=5)
    return mesh

def main():
    target_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Models", "Generated"))
    os.makedirs(target_dir, exist_ok=True)

    models = [
        ("Fisherman_Character.obj", build_fisherman()),
        ("Wrangler_Character.obj", build_wrangler()),
        ("WorldPortal_Gate.obj", build_world_portal()),
        ("Fish_ArcticTuna.obj", build_arctic_tuna()),
        ("Fish_SnowCrab.obj", build_snow_crab()),
        ("Fish_Clownfish.obj", build_clownfish()),
        ("Fish_Pufferfish.obj", build_pufferfish()),
        ("Sunken_Treasure_Chest.obj", build_treasure_chest()),
    ]

    for filename, mesh in models:
        out_path = os.path.join(target_dir, filename)
        mesh.save_obj(out_path)

    print(f"All {len(models)} models generated successfully in {target_dir}!")

if __name__ == "__main__":
    main()
