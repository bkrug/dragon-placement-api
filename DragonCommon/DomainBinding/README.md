This folder contains DTOs that allow data to be sent from one domain to another.
There should not be actual logic in this folder.
For now, the domains still seem to be simple enough and separate enough that I haven't written an anti-corruption layer.

Shared content between domains:
- https://martinfowler.com/bliki/BoundedContext.html
- https://www.oreilly.com/library/view/what-is-domain-driven/9781492057802/ch04.html
- https://deviq.com/domain-driven-design/shared-kernel/
- https://medium.com/@iamprovidence/relationships-between-bounded-contexts-in-ddd-ce5cfe3aaa04