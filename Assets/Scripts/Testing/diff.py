a = "EEDEEFJTT"
b = "JJJJJDFHT"

# state -1 unset 0- normal 1- modified 2- addition 3- removal

clookup = ['n', 'm', 'a', 'r', '_']

class char:
	def __init__(self, c):
		self.c = c
		self.state = -1
	def __repr__(self):
		return f'{self.c}{clookup[self.state]}'
	def __str__(self):
		return repr(self)
	def copy(self):
		new = char(self.c)
		new.state = self.state

		return new

def thing2(A, B):

	Arems = 0
	Badds = 0
	shifted = [i.copy() for i in B]
	i = 0
	
	for i in range(len(A)):
		cur = A[i]
		print(cur)
		dlist(A)
		dlist(B)

		# try to find it
		mi = i - Arems

		index = -1    
		for j in range(mi, len(shifted)):
			if shifted[j].c == cur.c:
				index = j
				break

		# print(shifted)
		print(f'i{i} {cur.c} inde {index}')
		
		if index == mi:
			A[i].state = 0
			B[mi + Badds].state = 0
			pass #normal

		if index > mi:
			# additions in B
			prerem = Badds
			for a in range(i, index):
				B[a + prerem].state = 2
				print(f'b {B[a + prerem]} at {a+prerem}')

				q = shifted.pop(i)
				print(f' pop {q}')
				
				Badds += 1
			
			A[i].state = 0
			B[mi + Badds].state = 0

		if index == -1:
			# no find to the right (in shifted)
			# look left and mark precedings as deleted if found lefter

			found = False
			lookleft = mi
			checked = 0

			if mi < len(B):
				while lookleft >= 0:
					lookleft -= 1
					checked += 1

					if B[lookleft + Badds].state == 0:
						break

					if shifted[lookleft].c == cur.c:
						print(f'found {lookleft}')
						found = True
						break

			if found:
				print(f' f, {checked}')
				for s in range(checked):
					A[i - s - 1].state = 3
					print(f'set {A[i-s-1]}')
					Arems += 1
				B[lookleft + Badds].state = 0
				A[i].state = 0
			else:
				print(f'funky {i=} {mi=} {Badds=} {len(B)=}')
				if mi + Badds >= len(B):
					# end of B, all following in A is removed
					for r in range(i, len(A)):
						A[r].state = 3
					
					break
				else:
					# ok
					A[i].state = 1
					B[mi + Badds].state = 1

	endI = mi + Badds + 1
	if endI < len(B):
		print(f'{i=} {mi=} {len(B)=} {Badds=} {Arems=}')
		for f in range(endI, len(B)):
			B[f].state = 2

	return A, B


def dlist(list):
	print(' '.join([str(i) for i in list]))

A = [char(c) for c in a]
B = [char(c) for c in b]

An, Bn = thing2(A, B)

# Ai, Bi = thing(B, A)

print()
dlist(An)
dlist(Bn)
# print()
# dlist(Ai)
# dlist(Bi)