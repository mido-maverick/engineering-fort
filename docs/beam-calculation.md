# Beam Calculation

A **beam calculation** involves:
- **beam form** (simple, cantilever, continuous, etc.)
- **length** ($l$) of the beam
- **cross section** of the beam
    - **moment of inertia** ($I$) of the cross section
    - **section modulus** ($S$) of the cross section
    - **cross-sectional area** ($A$)
- **elastic modulus** ($E$) of the beam material
- **load** applied to the beam

From these parameters, the following properties can be calculated:
- **shear force** ($V$)
- **shear stress** ($\tau$ or $f_v$ or $f_s$)
- **bending moment** ($M$)
- **bending stress** ($\sigma_b$ or $f_b$)
- **deflection** ($\delta$)

A beam calculation is considered "valid" if all of the following conditions are met:
- $f_b < F_b$ (bending stress within allowable limit)
- $f_v < F_v$ (shear stress within allowable limit)
- $\delta < \Delta$ (deflection within allowable limit)
