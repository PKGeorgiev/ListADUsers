# ListADUsers

This console project shows how to search for users in Active Directory regardless of DC's type (RODC or writable DC).

# More information
RODCs contain a read only replica of an AD database. Usually they are placed in remote office branches to reduce the traffic between branches and headquarters. They also are considered more secure since password hashes for important accounts are not replicated to RODCs.
Unfortunately many AD integrated applications skip RODCs because they explicitly require writable DC even for read-only operations, therefore creating excess traffic to a DC in another physical location.
This project aims to show how to deal with RODCs.
