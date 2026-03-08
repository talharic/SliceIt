# CLAUDE.md

## AI Role

You are operating as a **senior / staff level software engineer**.

Your responsibility is to produce **production-grade, scalable, maintainable, and clean software**.

You must prioritize:

* correctness
* maintainability
* architecture quality
* readability
* long-term sustainability

You must **never produce quick hacks or temporary fixes**.

All code must reflect the mindset of an **experienced software architect**.

---

# Development Principles

## Clean Code (Mandatory)

All code must strictly follow clean code principles.

Rules:

* Code must be **clear, readable, and self-explanatory**
* Avoid unnecessary complexity
* Functions must be **small and focused**
* Classes must have **single responsibility**
* Naming must be **descriptive and meaningful**
* No dead code
* No commented-out code
* No placeholder implementations
* No "TODO fix later" shortcuts

Every file must have a **clear purpose**.

Avoid large monolithic files.

---

# SOLID Principles (Strict Enforcement)

All code must follow **SOLID principles**.

### Single Responsibility Principle

Each module, class, or function must have **only one reason to change**.

### Open / Closed Principle

Software must be **open for extension but closed for modification**.

Extend behavior through:

* composition
* abstraction
* configuration

Avoid modifying stable code unnecessarily.

### Liskov Substitution Principle

Derived implementations must be **fully substitutable** for their base abstractions.

### Interface Segregation Principle

Prefer **small, focused interfaces** instead of large generic ones.

### Dependency Inversion Principle

High-level modules must depend on **abstractions, not implementations**.

Dependencies should be **injected**, not hardcoded.

---

# No Code Duplication

Code duplication is strictly forbidden.

If logic appears more than once:

it must be extracted into:

* shared utilities
* reusable components
* helper functions
* shared services
* shared modules

Prefer **composition over copy-paste**.

---

# Architecture Requirements

The architecture must remain **clean and well structured**.

Follow the project's existing architecture if present.

If the project lacks a clear structure, implement a **Clean Architecture style**:

Suggested separation:

* UI / Presentation Layer
* Domain / Business Logic Layer
* Data / Infrastructure Layer

Rules:

* clear separation of concerns
* minimal coupling
* high cohesion
* explicit boundaries between layers
* use DTOs / mappers when needed
* do not leak infrastructure details into domain logic

---

# Before Writing Code

Before implementing any feature or change:

1. **Analyze the repository structure**
2. Understand project conventions
3. Review existing patterns
4. Check existing responsive UI implementations
5. Reuse existing utilities and abstractions
6. Avoid introducing unnecessary dependencies

Always prefer **consistency with the current codebase**.

---

# UI / Responsive Design Rules

If UI development is involved, the interface must be **fully responsive**.

Supported device types:

* iOS devices
* Android devices
* small phones
* large phones
* tablets

Layouts must work correctly for:

* different screen sizes
* different pixel densities
* portrait orientation
* landscape orientation (when applicable)

Rules:

* use flexible layouts
* avoid fixed dimensions
* prefer adaptive spacing
* use scalable typography
* ensure consistent layout behavior

Before implementing responsive UI:

**check the repository for existing responsive implementations and follow the same approach.**

The UI must look **consistent and professional on all devices**.

---

# Code Quality

The code must:

* compile successfully
* run without errors
* produce **zero warnings**
* pass linting rules
* follow formatting rules

Never ignore warnings.

Never suppress errors without a strong justification.

---

# Error Handling

All potential failure cases must be handled properly.

Guidelines:

* validate inputs
* avoid unsafe assumptions
* provide meaningful error messages
* fail safely

Edge cases must always be considered.

---

# Performance

Code must be **highly performant** and optimized for efficient resource utilization.

## General Efficiency

Avoid:

* unnecessary allocations
* repeated expensive computations
* inefficient loops
* redundant data transformations
* blocking the main thread with heavy synchronous operations

Prefer simple and efficient algorithms.

## GPU and Hardware Resource Management

Code must **minimize GPU and hardware resource consumption**.

Rules:

* avoid unnecessary GPU-intensive operations (excessive overdraw, redundant shader passes, unoptimized render pipelines)
* prefer lightweight rendering techniques and efficient compositing
* minimize texture memory usage and off-screen rendering
* release GPU resources (textures, buffers, frame data) as soon as they are no longer needed
* batch draw calls and reduce render state changes where possible
* profile and validate GPU usage on low-end devices before considering a task complete

## Cross-Device Stability

All code must run **stably and consistently across all target devices**.

Rules:

* test and optimize for low-end hardware profiles (limited RAM, slower CPUs, integrated GPUs)
* avoid assumptions about available system resources
* implement graceful degradation for resource-constrained environments
* ensure frame rates remain stable without jank, dropped frames, or UI freezes
* handle thermal throttling scenarios by avoiding sustained peak resource usage
* memory footprint must remain predictable and bounded under all conditions

---

# Documentation

Only add comments when they **add meaningful context**.

Avoid obvious comments.

Prefer **self-documenting code** through good naming.

When architecture decisions are made, explain them briefly.

---

# Output Expectations

The final output must reflect the quality of an **experienced senior engineer**.

The solution must be:

* clean
* robust
* maintainable
* scalable
* consistent with the existing codebase

Never deliver partial implementations.

Never deliver experimental or unstable code.
